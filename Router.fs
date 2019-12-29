module AllerRetour.Router

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.JwtBearer

open Serilog
open Giraffe

open TwoTrackResult
open Input

let authorize : HttpHandler =
  requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let successSendMail mail =
  eitherTeeResult (fun _ -> Mail.send mail) ignore

let logger = Log.Logger

let eitherLog onOk onError =
  eitherTeeResult (onOk >> logger.Information) (onError >> logger.Information)

let tryRegister (input: RegistrationRequest.T) =
  result {
    do!  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne = None
      |> failIfFalse ["Email is already registered"]
      |> AppError.specify ConflictError

    return Command.registerCustomer input
  }

let tryAuthenticate (input: AuthenticationRequest.T) =
  result {
    let! customer
      =  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne
      |> failIfNone ["User with that email is not found"]
      |> AppError.specify NotFoundError

    do!  Pbkdf2.verify customer.PasswordHash input.Password
      |> failIfFalse ["Invalid password"]
      |> AppError.specify UnauthorizedError

    return Auth.generateToken customer
  }

let jsonWithCode code x = setStatusCode code >=> text (Json.serialize x)

let onFailure (AppError (case, x)) =
  match case with
  | ValidationError   -> jsonWithCode 400 x
  | UnauthorizedError -> jsonWithCode 401 x
  | NotFoundError     -> jsonWithCode 404 x
  | ConflictError     -> jsonWithCode 409 x
  | FatalError        -> jsonWithCode 500 ["Server error"]

let resultToHandler x = either (jsonWithCode 200) onFailure x

let createHandler validator switch input =
  warbler (
    fun _ ->
      try
        match validator input with
        | Success s -> switch s
        | Failure f -> AppError.create ValidationError f |> fail
      with
      | ex -> AppError.create FatalError [ex.Message] |> fail
      |> resultToHandler
  )

let registrationHandler =
  createHandler RegistrationRequest.validate tryRegister

let authenticationHandler =
  createHandler AuthenticationRequest.validate tryAuthenticate

let handleGetSecured =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    let id = ctx.User.FindFirst Auth.customerIdClaim

    text ("User " + id.Value + " is authorized to access this resource.") next ctx

let badRequest _ = jsonWithCode 400 ["Bad request"]

let inline jsonBind (handler : ^T -> HttpHandler) = Json.tryBind badRequest handler

let createApp () : HttpHandler =
  subRoute "/customer" (
    POST >=> choose [
      route "/register" >=> jsonBind registrationHandler
      route "/auth"     >=> jsonBind authenticationHandler
      route "/test"     >=> authorize >=> handleGetSecured
    ]
  )
