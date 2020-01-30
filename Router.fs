module AllerRetour.Router

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.JwtBearer
open FSharp.Control.Tasks.V2.ContextInsensitive

open Giraffe

open TwoTrackResult
open RequestTypes
open ResponseTypes
open Logger

let tryCatchR fFailure f = tryCatch succeed (fFailure >> fail) f

let toLogs = function
  | EmailIsAlreadyRegistered e -> sprintf "Invalid registration attempt: %s" e
  | CustomerNotFound e
    -> sprintf "Invalid authentication attempt. Email is not registered: %s" e
  | TokenNotFound e
    -> sprintf "Invalid email confirmation attempt. Token not found. %s" e
  | InvalidPassword e
    -> sprintf "Invalid authentication attempt. Invalid password. %s" e
  | Validation _ -> ""
  | DbError e -> e

let failureLog e =
  let level x =
    match x with
    | EmailIsAlreadyRegistered _ | CustomerNotFound _
    | InvalidPassword _ | TokenNotFound _ -> logger.Information
    | DbError _ -> logger.Error
    | Validation _ -> logger.Debug

  either succeed (fun x -> level x (toLogs x); fail x) e

let handleError = function
  | EmailIsAlreadyRegistered _ -> Status.conflictError "Email is already registered"
  | CustomerNotFound _         -> Status.notFoundError "Customer not found"
  | TokenNotFound _            -> Status.notFoundError "Token not found"
  | InvalidPassword _          -> Status.unauthorizedError "Invalid password"
  | Validation ers             -> Status.validationError ers
  | DbError _                  -> Status.serverError


// For those, who passed registration, but didn't confirm their emails yet
let authorizeDefault : HttpHandler =
  requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

 // For fully priveleged users
let authorizeConfirmed : HttpHandler =
  requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)
  >=> authorizeByPolicyName
        Auth.mustHaveConfirmedEmailPolicy
        (Status.unauthorizedError "Unauthorized")

let bindCustomerIdentity (handler: CustomerIdentity -> HttpHandler) : HttpHandler =
  fun (next: HttpFunc) (ctx: HttpContext) ->
    let customerIdentity = {
      Id = ctx.User.FindFirst(Auth.customerIdClaim).Value |> Int64.Parse
      Email = ctx.User.FindFirst(Auth.emailClaim).Value
    }
    handler customerIdentity next ctx

let inline tryBindJson (successHandler: ^T -> HttpHandler) : HttpHandler =
  fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
      let! payload = ctx.ReadBodyFromRequestAsync()
      let  res     = Json.deserialize payload

      return!
        (match res with
        | Choice1Of2 dto -> successHandler dto
        | Choice2Of2 _   -> Status.validationError "Bad request") next ctx
    }

let tryBindQuery (successHandler: 'T -> HttpHandler)
  = tryBindQuery (ignore2 (Status.validationError "Bad request")) None successHandler

let toHandler x
  =  x
  |> either Status.ok handleError
  |> ignore2
  |> warbler

let sendConfirmEmail (customer: Db.Customer) =
  try
    let token = Command.createConfirmationToken customer.Email
    Mail.sendConfirm customer.Email token.Token
  with
  | exn -> logger.Error(exn.Message)

let trySignUp (input: SignUpRequest) =
  result {
    do!  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne
      |> failIfSome (EmailIsAlreadyRegistered input.Email)

    return Command.registerCustomer input
  }

let trySignIn (input: SignInRequest) =
  result {
    let! customer
      =  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne
      |> failIfNone (CustomerNotFound input.Email)

    do!  Pbkdf2.verify customer.PasswordHash input.Password
      |> failIfFalse (InvalidPassword input.Email)

    let token, expires
      = Auth.generateToken customer.Id customer.EmailConfirmed customer.Email

    return {
      Token = token
      EmailConfirmed = customer.EmailConfirmed
      Expires = expires
    }
  }

let tryConfirmEmail (input: ConfirmEmailRequest) =
  result {
    let! customer
      =  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne
      |> failIfNone (CustomerNotFound input.Email)

    let! token
      =  Query.emailConfirmationToken input.Email input.Code
      |> Seq.tryExactlyOne
      |> failIfNone (TokenNotFound input.Email)

    do!
      try
        Command.confirmEmail customer token |> succeed
      with
      | e -> DbError e.Message |> fail

    return "Email confirmed"
  }

let tryGetProfile (identity: CustomerIdentity) =
  result {
    let! customer
      =  Query.customerById identity.Id
      |> Seq.tryExactlyOne
      |> failIfNone (CustomerNotFound identity.Email)

    let! profile
      =  customer.``public.customer_profiles by id``
      |> Seq.tryExactlyOne
      |> failIfNone (CustomerNotFound identity.Email)

    return {
      Email = customer.Email
      CardId = customer.CardId
      FirstName = profile.FirstName
      LastName = profile.LastName
      Birthday = profile.Birthday
      Gender = profile.Gender
    }
  }

let signUpHandler : HttpHandler
  =  SignUpRequest.validate
  >> bind trySignUp
  >> failureLog
  >> map (tee sendConfirmEmail)
  >> map (fun x -> x.Id)
  >> toHandler
  |> tryBindJson

let signInHandler : HttpHandler
  =  SignInRequest.validate
  >> bind trySignIn
  >> failureLog
  >> toHandler
  |> tryBindJson

let confirmEmailHandler : HttpHandler
  =  ConfirmEmailRequest.validate
  >> bind tryConfirmEmail
  >> toHandler
  |> tryBindQuery

let getProfileHandler : HttpHandler
  =  tryGetProfile
  >> toHandler
  |> bindCustomerIdentity

let createApp () : HttpHandler =
  subRoute "/api" (
    subRoute "/customer" (
      choose [
        POST >=> choose [
          route "/signup" >=> signUpHandler
          route "/signin" >=> signInHandler
        ]
        GET >=> choose [
          route "/confirm" >=> confirmEmailHandler

          authorizeConfirmed >=> choose [
            route "/profile" >=> getProfileHandler
          ]
        ]
        Status.notFoundError "Not found"
      ]
    )
  )
