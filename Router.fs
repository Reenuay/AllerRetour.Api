module AllerRetour.Router

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.JwtBearer

open Serilog
open Giraffe

open TwoTrackResult
open Input

let logger = Log.Logger

let tryCatchR f x =
  tryCatch id (List.wrap >> AppError.create Fatal >> fail) f x

let authorize : HttpHandler =
  requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let successSendMail mail =
  eitherTeeR (Mail.send mail |> ignore2) ignore

let tryRegister (input: RegistrationRequest.T) =
  result {
    do!  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne = None
      |> failIfFalse ["Email is already registered"]
      |> AppError.specify Conflict

    return Command.registerCustomer input
  }

let tryAuthenticate (input: AuthenticationRequest.T) =
  result {
    let! customer
      =  Query.customerByEmail input.Email
      |> Seq.tryExactlyOne
      |> failIfNone ["User with that email is not found"]
      |> AppError.specify NotFound

    do!  Pbkdf2.verify customer.PasswordHash input.Password
      |> failIfFalse ["Invalid password"]
      |> AppError.specify Unauthorized

    return Auth.generateToken customer
  }

let jsonWithCode code x = setStatusCode code >=> text (Json.serialize x)

let onFailure (AppError (case, x)) =
  match case with
  | Validation   -> jsonWithCode 400 x
  | Unauthorized -> jsonWithCode 401 x
  | NotFound     -> jsonWithCode 404 x
  | Conflict     -> jsonWithCode 409 x
  | Fatal        -> jsonWithCode 500 ["Server error"]

let resultToHandler x = either (jsonWithCode 200) onFailure x

let createHandler validator switch
  =  validator
  >> either (tryCatchR switch) (AppError.create Validation >> fail)
  >> resultToHandler
  >> ignore2
  >> warbler

let registrationHandler = createHandler RegistrationRequest.validate tryRegister

let authenticationHandler = createHandler AuthenticationRequest.validate tryAuthenticate

let handleGetSecured =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    let id = ctx.User.FindFirst Auth.customerIdClaim

    text ("User " + id.Value + " is authorized to access this resource.") next ctx

let badRequest = jsonWithCode 400 ["Bad request"] |> ignore2

let inline jsonBind (handler : ^T -> HttpHandler) = Json.tryBind badRequest handler

let createApp () : HttpHandler =
  subRoute "/customer" (
    POST >=> choose [
      route "/register" >=> jsonBind registrationHandler
      route "/auth"     >=> jsonBind authenticationHandler
      route "/test"     >=> authorize >=> handleGetSecured
    ]
  )
