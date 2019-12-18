module Router

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.JwtBearer

open Giraffe

open Auth
open Input
open ResultUtils

type AppSettings = {
  Auth: AuthSettings
}

let jsonWithCode code x = setStatusCode code >=> text (Json.serialize x)

let authorize : HttpHandler =
  requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let onError = function
| Validation e -> jsonWithCode 400 e
| NotFound   e -> jsonWithCode 404 e
| Conflict   e -> jsonWithCode 409 e
| Fatal      e ->
  printfn "%A" e // TO DO: Add Logs!
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
      | ex -> Fatal [ex.Message] |> Error
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

let registrationHandler = createHandler RegistrationRequest.validate tryRegister

let createAuthenticationHandler generateToken =
  createHandler AuthenticationRequest.validate (tryAuthenticate generateToken)

let handleGetSecured =
  fun (next : HttpFunc) (ctx : HttpContext) ->
    let id = ctx.User.FindFirst customerIdClaim

    text ("User " + id.Value + " is authorized to access this resource.") next ctx

let createApp (settings) : HttpHandler =
  let authenticationHandler = createAuthenticationHandler (generateToken settings.Auth)

  subRoute "/customer" (
    POST >=> choose [
      route "/register" >=> jsonBind registrationHandler
      route "/auth"     >=> jsonBind authenticationHandler
      route "/test"     >=> authorize >=> handleGetSecured
    ]
  )
