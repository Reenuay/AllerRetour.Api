module Router

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.JwtBearer

open Serilog
open Giraffe

open Auth
open Input
open ResultUtils

let logger = Log.Logger

let logResult onOk onError = function
  | Ok o    -> onOk o    |> logger.Information; Ok o
  | Error e -> onError e |> logger.Information; Error e

type AppSettings = {
  Auth: AuthSettings
}

let jsonWithCode code x = setStatusCode code >=> text (Json.serialize x)

let authorize : HttpHandler =
  requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let onError (AppError (case, x)) =
  match case with
  | ValidationError -> jsonWithCode 400 x
  | NotFoundError   -> jsonWithCode 404 x
  | ConflictError   -> jsonWithCode 409 x
  | FatalError      ->
    logger.Error(Json.serialize x)
    jsonWithCode 500 ["Oops! Something went wrong..."]

let resultToHandler = function
| Ok    o -> jsonWithCode 200 o
| Error e -> onError e

let badRequest _ = jsonWithCode 400 ["Bad request"]

let inline jsonBind (handler : ^T -> HttpHandler) = Json.tryBind badRequest handler

let createHandler validator switch input =
  warbler (
    fun _ ->
      try
        match validator input with
        | Ok o    -> switch o
        | Error e -> Error e |> toValidationError
      with
      | ex -> listToAppError FatalError [ex.Message]
      |> resultToHandler
  )

let tryRegister (input: RegistrationRequest.T) =
  result {
    do!  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne = None
      |> falseTo ["Email is already registered"]
      |> toConflictError

    return Command.registerCustomer input
  }
  |> logResult
    (fun _ -> sprintf "Registration successful: %s" input.Email)
    (fun e -> sprintf "Registration failed: %s %s" input.Email (toList e |> Json.serialize))

let tryAuthenticate settings (input: AuthenticationRequest.T) =
  result {
    let! customer
      =  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne
      |> fromOption ["User with that email is not found"]
      |> toNotFoundError

    do!  Pbkdf2.verify customer.PasswordHash input.Password
      |> falseTo ["Invalid password"]
      |> toValidationError

    return generateToken settings customer
  }
  |> logResult
    (fun _ -> sprintf "Auth successful: %s" input.Email)
    (fun e -> sprintf "Auth failed: %s %s" input.Email (toList e |> Json.serialize))

let registrationHandler = createHandler RegistrationRequest.validate tryRegister

let authenticationHandler generateToken =
  createHandler AuthenticationRequest.validate (tryAuthenticate generateToken)

let handleGetSecured =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    let id = ctx.User.FindFirst customerIdClaim

    text ("User " + id.Value + " is authorized to access this resource.") next ctx

let createApp (settings) : HttpHandler =
  subRoute "/customer" (
    POST >=> choose [
      route "/register" >=> jsonBind registrationHandler
      route "/auth"     >=> jsonBind (authenticationHandler settings.Auth)
      route "/test"     >=> authorize >=> handleGetSecured
    ]
  )
