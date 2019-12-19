module Router

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.JwtBearer

open Giraffe

open Auth
open Input
open ResultUtils

type LogLevel =
  | VerboseLevel
  | DebugLevel
  | InfoLevel
  | WarningLevel
  | ErrorLevel
  | FatalLevel

type AppSettings = {
  Auth: AuthSettings
  Log: LogLevel -> string -> unit
}

let jsonWithCode code x = setStatusCode code >=> text (Json.serialize x)

let authorize : HttpHandler =
  requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let onError log = function
| ValidationError e -> jsonWithCode 400 e
| NotFoundError   e -> jsonWithCode 404 e
| ConflictError   e -> jsonWithCode 409 e
| FatalError      e ->
  log ErrorLevel (Json.serialize e)
  jsonWithCode 500 ["Oops! Something went wrong..."]

let resultToHandler log = function
| Ok    o -> jsonWithCode 200 o
| Error e -> onError log e

let badRequest _ = jsonWithCode 400 ["Bad request"]

let inline jsonBind (handler : ^T -> HttpHandler) = Json.tryBind badRequest handler

let createHandler log validator switch input =
  warbler (
    fun _ ->
      try
        match validator input with
        | Ok o    -> switch o
        | Error e -> Error e |> toValidationError
      with
      | ex -> FatalError [ex.Message] |> Error
      |> resultToHandler log
  )

let tryRegister (input: RegistrationRequest.T) =
  result {
    do!  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne = None
      |> falseTo ["Email is already registered"]
      |> toConflictError

    return Command.registerCustomer input
  }

let tryAuthenticate generateToken (input: AuthenticationRequest.T) =
  result {
    let! customer
      =  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne
      |> fromOption ["User with that email is not found"]
      |> toNotFoundError

    do!  Pbkdf2.verify customer.PasswordHash input.Password
      |> falseTo ["Invalid password"]
      |> toValidationError

    return generateToken customer
  }

let createRegistrationHandler log = createHandler log RegistrationRequest.validate tryRegister

let createAuthenticationHandler log generateToken =
  createHandler log AuthenticationRequest.validate (tryAuthenticate generateToken)

let handleGetSecured =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    let id = ctx.User.FindFirst customerIdClaim

    text ("User " + id.Value + " is authorized to access this resource.") next ctx

let createApp (settings) : HttpHandler =
  let log = settings.Log

  let registrationHandler   = createRegistrationHandler log
  let authenticationHandler = createAuthenticationHandler log (generateToken settings.Auth)

  subRoute "/customer" (
    POST >=> choose [
      route "/register" >=> jsonBind registrationHandler
      route "/auth"     >=> jsonBind authenticationHandler
      route "/test"     >=> authorize >=> handleGetSecured
    ]
  )
