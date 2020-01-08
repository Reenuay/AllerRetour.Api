module AllerRetour.Router

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.JwtBearer
open FSharp.Control.Tasks.V2.ContextInsensitive

open Giraffe

open TwoTrackResult
open Input
open Logger

let tryCatchR fFailure f = tryCatch succeed (fFailure >> fail) f

let authorize : HttpHandler =
  requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let toLogs = function
  | EmailIsAlreadyRegistered e -> sprintf "Invalid registration attempt: %s" e
  | EmailIsNotConfirmed e
    -> sprintf "Invalid authentication attempt. Email is not confirmed: %s" e
  | CustomerNotFound e
    -> sprintf "Invalid authentication attempt. Email is not registered: %s" e
  | InvalidPassword e
    -> sprintf "Invalid authentication attempt. Invalid password. %s" e
  | Validation _ -> ""
  | DbError e -> e

let failureLog e =
  let level x =
    match x with
    | EmailIsAlreadyRegistered _ | EmailIsNotConfirmed _
    | CustomerNotFound _ | InvalidPassword _ -> logger.Information
    | DbError _ -> logger.Error
    | Validation _ -> logger.Debug

  either succeed (fun x -> level x (toLogs x); fail x) e

let handleError = function
  | EmailIsAlreadyRegistered _ -> Status.conflictError "Email is already registered"
  | CustomerNotFound _         -> Status.notFoundError "Customer not found"
  | EmailIsNotConfirmed _      -> Status.unauthorizedError "Email is not confirmed"
  | InvalidPassword _          -> Status.unauthorizedError "Invalid password"
  | Validation ers             -> Status.validationError ers
  | DbError _                  -> Status.serverError

let inline json (successHandler: ^T -> HttpHandler) : HttpHandler =
  fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
      let! payload = ctx.ReadBodyFromRequestAsync()
      let  res     = Json.deserialize payload

      return!
        (match res with
        | Choice1Of2 dto -> successHandler dto
        | Choice2Of2 _   -> Status.validationError "Bad request") next ctx
    }

let toHandler x
  =  x
  |> either Status.ok handleError
  |> ignore2
  |> warbler

let sendEmail (customer: Dto.Customer) =
  try
    let code = Command.createConfirmationToken customer.Email
    Mail.sendConfirm customer.Email code
  with
  | exn -> logger.Error(exn.Message)

  customer

let tryRegister (input: RegRequest.T) =
  result {
    do!  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne
      |> failIfSome (EmailIsAlreadyRegistered input.Email)

    return Command.registerCustomer input
  }

let tryAuthenticate (input: AuthRequest.T) =
  result {
    let! customer
      =  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne
      |> failIfNone (CustomerNotFound input.Email)

    do!  customer.EmailConfirmed
      |> failIfFalse (EmailIsNotConfirmed input.Email)

    do!  Pbkdf2.verify customer.PasswordHash input.Password
      |> failIfFalse (InvalidPassword input.Email)

    return Auth.generateToken customer
  }

let registrationHandler
  =  RegRequest.validate
  >> bind tryRegister
  >> failureLog
  >> map sendEmail
  >> map (fun x -> x.Id)
  >> toHandler
  |> json

let authenticationHandler
  =  AuthRequest.validate
  >> bind tryAuthenticate
  >> failureLog
  >> toHandler
  |> json

let createApp () : HttpHandler =
  subRoute "/api" (
    subRoute "/customer" (
      choose [
        POST >=> choose [
          route "/register" >=> registrationHandler
          route "/auth"     >=> authenticationHandler
        ]
        Status.notFoundError "Not found"
      ]
    )
  )
