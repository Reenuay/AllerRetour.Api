module AllerRetour.Router

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.JwtBearer
open FSharp.Control.Tasks.V2.ContextInsensitive

open Giraffe

open TwoTrackResult
open Input
open Output
open Logger

let tryCatchR fFailure f = tryCatch succeed (fFailure >> fail) f

// For those, who passed registration, but didn't confirm their emails yet
let authorizeUnconfirmed : HttpHandler =
  requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

 // For fully priveleged users
let authorizeConfirmed : HttpHandler
  =   authorizeUnconfirmed
  >=> authorizeByPolicyName
        Auth.mustHaveConfirmedEmailPolicy
        (Status.unauthorizedError "Unauthorized")

let toLogs = function
  | EmailIsAlreadyRegistered e -> sprintf "Invalid registration attempt: %s" e
  | EmailIsNotConfirmed e
    -> sprintf "Invalid authentication attempt. Email is not confirmed: %s" e
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
    | EmailIsAlreadyRegistered _ | EmailIsNotConfirmed _
    | CustomerNotFound _ | InvalidPassword _ | TokenNotFound _ -> logger.Information
    | DbError _ -> logger.Error
    | Validation _ -> logger.Debug

  either succeed (fun x -> level x (toLogs x); fail x) e

let handleError = function
  | EmailIsAlreadyRegistered _ -> Status.conflictError "Email is already registered"
  | CustomerNotFound _         -> Status.notFoundError "Customer not found"
  | TokenNotFound _            -> Status.notFoundError "Token not found"
  | EmailIsNotConfirmed _      -> Status.unauthorizedError "Email is not confirmed"
  | InvalidPassword _          -> Status.unauthorizedError "Invalid password"
  | Validation ers             -> Status.validationError ers
  | DbError _                  -> Status.serverError

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

let trySignUp (input: RegRequest.T) =
  result {
    do!  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne
      |> failIfSome (EmailIsAlreadyRegistered input.Email)

    return Command.registerCustomer input
  }

let trySignIn (input: AuthRequest.T) =
  result {
    let! customer
      =  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne
      |> failIfNone (CustomerNotFound input.Email)

    do!  customer.EmailConfirmed
      |> failIfFalse (EmailIsNotConfirmed input.Email)

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

let tryConfirmEmail (input: EmailConfirmRequest.T) =
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

let signUpHandler : HttpHandler
  =  RegRequest.validate
  >> bind trySignUp
  >> failureLog
  >> map (tee sendConfirmEmail)
  >> map (fun x -> x.Id)
  >> toHandler
  |> tryBindJson

let signInHandler : HttpHandler
  =  AuthRequest.validate
  >> bind trySignIn
  >> failureLog
  >> toHandler
  |> tryBindJson

let confirmEmailHandler : HttpHandler
  =  EmailConfirmRequest.validate
  >> bind tryConfirmEmail
  >> toHandler
  |> tryBindQuery

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
        ]
        Status.notFoundError "Not found"
      ]
    )
  )
