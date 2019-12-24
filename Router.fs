module Router

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.JwtBearer

open Serilog
open Giraffe

open Input
open ResultUtils

// --------------AUTH--------------
let authorize : HttpHandler =
  requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

// --------------EMAIL SENDING--------------
let sendEmailIfOk mail =
  side (fun _ -> Mail.send mail) ignore

// --------------LOGGING--------------
let logger = Log.Logger

let logResult onOk onError =
  side (onOk >> logger.Information) (onError >> logger.Information)

// --------------RESULT WORKFLOWS--------------
let tryRegister (input: RegistrationRequest.T) =
  result {
    do!  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne = None
      |> falseTo ["Email is already registered"]
      |> toConflictError

    return Command.registerCustomer input
  }
  |> sendEmailIfOk { To = input.Email; Subject = "Test"; Body = "Testing email sending!" }
  |> logResult
    (fun _ -> sprintf "Registration successful: %s" input.Email)
    (fun e -> sprintf "Registration failed: %s %s" input.Email (toList e |> Json.serialize))

let tryAuthenticate (input: AuthenticationRequest.T) =
  result {
    let! customer
      =  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne
      |> fromOption ["User with that email is not found"]
      |> toNotFoundError

    do!  Pbkdf2.verify customer.PasswordHash input.Password
      |> falseTo ["Invalid password"]
      |> toUnauthorizedError

    return Auth.generateToken customer
  }
  |> logResult
    (fun _ -> sprintf "Auth successful: %s" input.Email)
    (fun e -> sprintf "Auth failed: %s %s" input.Email (toList e |> Json.serialize))

// --------------HANDLERS--------------
let jsonWithCode code x = setStatusCode code >=> text (Json.serialize x)

let onError (AppError (case, x)) =
  match case with
  | ValidationError -> jsonWithCode 400 x
  | Unauthorized    -> jsonWithCode 401 x
  | NotFoundError   -> jsonWithCode 404 x
  | ConflictError   -> jsonWithCode 409 x
  | FatalError      ->
    logger.Error(Json.serialize x)
    jsonWithCode 500 ["Oops! Something went wrong..."]

let resultToHandler = function
| Ok    o -> jsonWithCode 200 o
| Error e -> onError e

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

let registrationHandler =
  createHandler RegistrationRequest.validate tryRegister

let authenticationHandler =
  createHandler AuthenticationRequest.validate tryAuthenticate

let handleGetSecured =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    let id = ctx.User.FindFirst Auth.customerIdClaim

    text ("User " + id.Value + " is authorized to access this resource.") next ctx

// --------------APP--------------
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
