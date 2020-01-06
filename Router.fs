module AllerRetour.Router

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.JwtBearer

open Giraffe

open TwoTrackResult
open Input
open Logger

let authorize : HttpHandler =
  requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let successSendMail mail =
  eitherTeeR (Mail.send mail |> ignore2) ignore

let tryCatchR fFailure f = tryCatch id (fFailure >> fail) f

let tryReg _ = // TO DO ADD SUCCESS LOGGING
  let f (input: RegRequest.T) = result {
    do!  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne
      |> failIfSome (EmailIsAlreadyRegistered input.Email)

    return Command.registerCustomer input
  }

  tryCatchR DbError f

let tryAuth _ = // TO DO ADD SUCCESS LOGGING
  let f (input: AuthRequest.T) =
    result {
      let! customer
        =  Query.customerByEmail input.Email
        |> Seq.tryExactlyOne
        |> failIfNone (CustomerNotFound input.Email)

      do!  Pbkdf2.verify customer.PasswordHash input.Password
        |> failIfFalse (InvalidPassword input.Email)

      return Auth.generateToken customer
    }

  tryCatchR DbError f

let toLogs = function
  | EmailIsAlreadyRegistered e -> sprintf "Invalid registration attempt: %s" e
  | CustomerNotFound e
    -> sprintf "Invalid authentication attempt. Email is not registered: %s" e
  | InvalidPassword e
    -> sprintf "Invalid authentication attempt. Invalid password. %s" e
  | Validation _ -> ""
  | DbError e -> e

let logError e =
  let method =
    match e with
    | EmailIsAlreadyRegistered _ | CustomerNotFound _ | InvalidPassword _ -> logger.Information
    | DbError _ -> logger.Error
    | Validation _ -> logger.Debug

  toLogs e |> method

  fail e

let handleError = function
  | EmailIsAlreadyRegistered _ -> Status.conflictError "Email is already registered"
  | CustomerNotFound _         -> Status.notFoundError "Customer not found"
  | InvalidPassword _          -> Status.unauthorizedError "Invalid password"
  | Validation ers             -> Status.validationError ers
  | DbError _                  -> Status.serverError

let resultToHandler x = either Status.ok handleError x

let createHandler validator switch input =
  let f x =
    input
    |> validator
    |> bindFailure (Validation >> fail)
    |> bind (switch x)
    |> bindFailure logError
    |> resultToHandler

  warbler f

let registrationHandler = createHandler RegRequest.validate tryReg

let authenticationHandler = createHandler AuthRequest.validate tryAuth

let inline jsonBind (handler : ^T -> HttpHandler)
  = Json.tryBind (Status.validationError "Bad request") handler

let createApp () : HttpHandler =
  subRoute "/customer" (
    POST >=> choose [
      route "/register" >=> jsonBind registrationHandler
      route "/auth"     >=> jsonBind authenticationHandler
    ]
  )
