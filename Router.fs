module AllerRetour.Router

open FSharp.Data.Sql
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Logger
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Http
open RequestTypes
open ResponseTypes
open System
open TwoTrackResult

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

let OK = "Ok"

let trySignIn (request: SignInRequest) =
  result {
    let! customer
      =  Query.customerByEmail request.Email
      |> Seq.tryExactlyOne
      |> failIfNone (CustomerNotFound request.Email)

    do!  Pbkdf2.verify customer.PasswordHash request.Password
      |> failIfFalse (InvalidPassword request.Email)

    let token, expires
      = Auth.generateToken customer.Id customer.EmailConfirmed customer.Email

    return {
      Token = token
      EmailConfirmed = customer.EmailConfirmed
      Expires = expires
    }
  }

let trySignUp (request: SignUpRequest) =
  result {
    do!  Query.customerByEmail request.Email
      |> Seq.tryExactlyOne
      |> failIfSome (EmailIsAlreadyRegistered request.Email)

    let customer = Command.registerCustomer request

    return customer.Email, Command.createConfirmationToken customer.Id
  }

let tryConfirmEmail (request: ConfirmEmailRequest) =
  result {
    let! customer
      =  Query.customerByEmail request.Email
      |> Seq.tryExactlyOne
      |> failIfNone (TokenNotFound request.Email)

    let! token
      =  Query.unexpiredEmailConfirmationToken customer.Id
      |> Seq.tryExactlyOne
      |> failIfNone (TokenNotFound request.Email)

    do!  Pbkdf2.verify token.TokenHash request.Token
      |> failIfFalse (TokenNotFound request.Email)

    Command.confirmEmail customer token

    return OK
  }

let trySendPasswordResetEmail (request: PasswordResetEmailRequest) =
  result {
    let! customer
      =  Query.customerByEmail request.Email
      |> Seq.tryExactlyOne
      |> failIfNone (CustomerNotFound request.Email)

    customer.``public.password_reset_tokens by id``
    |> Seq.``delete all items from single table``
    |> Async.RunSynchronously
    |> ignore

    return request.Email, Command.createResetToken customer.Id
  }

let tryPasswordReset (request: PasswordResetRequest) =
  result {
    let! customer
      =  Query.customerByEmail request.Email
      |> Seq.tryExactlyOne
      |> failIfNone (TokenNotFound request.Email)

    let! token
      =  Query.passwordResetToken customer.Id
      |> Seq.tryExactlyOne
      |> failIfNone (TokenNotFound customer.Email)

    do!  Pbkdf2.verify token.TokenHash request.Token
      |> failIfFalse (TokenNotFound customer.Email)

    Command.changePassword customer request.NewPassword

    return OK
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

let tryResendConfirmEmail (identity: CustomerIdentity) =
  result {
    let! customer
      =  Query.customerById identity.Id
      |> Seq.tryExactlyOne
      // TO DO: Return that user is already confirmed his email
      |> failIfNone (CustomerNotFound identity.Email)

    customer.``public.email_confirmation_tokens by id``
    |> Seq.``delete all items from single table``
    |> Async.RunSynchronously
    |> ignore

    return customer.Email, Command.createConfirmationToken customer.Id
  }

let tryUpdateProfile (identity: CustomerIdentity) (request: UpdateProfileRequest) =
  result {
    let! customer
      =  Query.customerById identity.Id
      |> Seq.tryExactlyOne
      |> failIfNone (CustomerNotFound identity.Email)

    let! profile
      =  customer.``public.customer_profiles by id``
      |> Seq.tryExactlyOne
      |> failIfNone (CustomerNotFound identity.Email)

    Command.updateProfile profile request

    return {
      Email = customer.Email
      CardId = customer.CardId
      FirstName = profile.FirstName
      LastName = profile.LastName
      Birthday = profile.Birthday
      Gender = profile.Gender
    }
  }

let tryChangeEmail (identity: CustomerIdentity) (request: ChangeEmailRequest) =
  result {
    let! customer
      =  Query.customerById identity.Id
      |> Seq.tryExactlyOne
      |> failIfNone (CustomerNotFound identity.Email)

    do!  Pbkdf2.verify customer.PasswordHash request.Password
      |> failIfFalse (InvalidPassword customer.Email)

    do!  customer.Email <> request.NewEmail
      |> failIfFalse (Validation ["This is the old email address. Nothing to change."])

    do!  Query.customerByEmail request.NewEmail
      |> Seq.tryExactlyOne
      |> failIfSome (EmailIsAlreadyRegistered request.NewEmail)

    customer.``public.email_confirmation_tokens by id``
    |> Seq.``delete all items from single table``
    |> Async.RunSynchronously
    |> ignore

    Command.changeEmail customer request.NewEmail

    return customer.Email, Command.createConfirmationToken customer.Id
  }

let tryChangePassword (identity: CustomerIdentity) (request: ChangePasswordRequest) =
  result {
    let! customer
      =  Query.customerById identity.Id
      |> Seq.tryExactlyOne
      |> failIfNone (CustomerNotFound identity.Email)

    do!  Pbkdf2.verify customer.PasswordHash request.OldPassword
      |> failIfFalse (InvalidPassword identity.Email)

    Command.changePassword customer request.NewPassword

    return OK
  }

let signInHandler : HttpHandler
  =  SignInRequest.validate
  >> bind trySignIn
  >> failureLog
  >> toHandler
  |> tryBindJson

let signUpHandler : HttpHandler
  =  SignUpRequest.validate
  >> bind trySignUp
  >> failureLog
  >> map (tee Mail.sendConfirm)
  >> map (ignore2 OK)
  >> toHandler
  |> tryBindJson

let confirmEmailHandler : HttpHandler
  =  ConfirmEmailRequest.validate
  >> bind tryConfirmEmail
  >> toHandler
  |> tryBindQuery

let sendPasswordResetEmailHandler : HttpHandler
  =  PasswordResetEmailRequest.validate
  >> bind trySendPasswordResetEmail
  >> map (tee Mail.sendReset)
  >> map (ignore2 OK)
  >> toHandler
  |> tryBindJson

let passwordResetHandler : HttpHandler
  =  PasswordResetRequest.validate
  >> bind tryPasswordReset // Add email notification
  >> toHandler
  |> tryBindJson

let getProfileHandler : HttpHandler
  =  tryGetProfile
  >> toHandler
  |> bindCustomerIdentity

let resendConfirmEmailHandler : HttpHandler
  =  tryResendConfirmEmail
  >> map (tee Mail.sendConfirm)
  >> map (ignore2 OK)
  >> toHandler
  |> bindCustomerIdentity

let updateProfileHanlder : HttpHandler
  = bindCustomerIdentity
      (fun id ->
        UpdateProfileRequest.validate
        >> bind (tryUpdateProfile id)
        >> toHandler
        |> tryBindJson)

let changeEmailHandler : HttpHandler
  = bindCustomerIdentity
      (fun id ->
        ChangeEmailRequest.validate
        >> bind (tryChangeEmail id)
        >> map (tee Mail.sendConfirm)
        >> map (ignore2 OK)
        >> toHandler
        |> tryBindJson)

let changePasswordHandler : HttpHandler
  = bindCustomerIdentity
      (fun id ->
        ChangePasswordRequest.validate
        >> bind (tryChangePassword id) // Add email notification
        >> toHandler
        |> tryBindJson)

let createApp () : HttpHandler =
  subRoute "/api" (
    subRoute "/customer" (
      choose [
        // TO DO: Protect from DOS attacks routes without authorization
        route "/signin" >=> POST >=> signInHandler
        route "/signup" >=> POST >=> signUpHandler

        subRoute "/password" (
          choose [
            route "/pin"    >=> POST >=> sendPasswordResetEmailHandler
            route "/reset"  >=> POST >=> passwordResetHandler
            route "/change" >=> authorizeConfirmed >=> POST >=> changePasswordHandler
          ]
        )

        subRoute "/email" (
          choose [
            route "/confirm" >=> GET >=> confirmEmailHandler
            route "/resend"  >=> authorizeDefault >=> POST >=> resendConfirmEmailHandler
            route "/change"  >=> authorizeDefault >=> POST >=> changeEmailHandler
          ]
        )

        route "/profile" >=> authorizeConfirmed >=> choose [
          GET >=> getProfileHandler
          PUT >=> updateProfileHanlder
        ]

        Status.notFoundError "Not found"
      ]
    )
  )
