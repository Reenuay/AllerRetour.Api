module Router

open Giraffe
open ResultUtils
open Input

let jsonWithCode code x = setStatusCode code >=> text (Json.serialize x)

let onError = function
| Validation e -> jsonWithCode 400 e
| NotFound   e -> jsonWithCode 404 e
| Conflict   e -> jsonWithCode 409 e
| Fatal      e ->
  printfn "%A" e // TO DO: Add Logs!
  jsonWithCode 500 "Oops! Something went wrong..."

let resultToHandler = function
| Ok    o -> jsonWithCode 200 o
| Error e -> onError e

let fromSwitch validator switch input =
  warbler (
    fun _ ->
      match validator input with
      | Ok o    -> switch o
      | Error e -> Error e |> toValidationError
      |> resultToHandler
  )

let registrationHandler =
  fromSwitch RegistrationRequest.validate (
    fun input -> result {

      do!  Query.customerByEmail input.Email
        |> Seq.tryExactlyOne = None
        |> falseTo ["Email is already registered"]
        |> toConflictError

      return! (tryCatch Command.registerCustomer input) |> toFatalError
    }
  )

let authenticationHandler =
  fromSwitch AuthenticationRequest.validate (
    fun input ->
      result {
        let! customer
          =  Query.customerByEmail input.Email
          |> Seq.tryExactlyOne
          |> fromOption ["User with that email is not found"]
          |> toNotFoundError

        do!  Pbkdf2.verify customer.PasswordHash input.Password
          |> falseTo ["Invalid password"]
          |> toValidationError

        return "Heysan"
      }
  )

let badRequest _ = RequestErrors.BAD_REQUEST "Bad request"

let inline jsonBind (handler : ^T -> HttpHandler) = Json.tryBind badRequest handler

let app : HttpHandler =
  subRoute "/customer" (
    POST >=> choose [
      route "/register" >=> jsonBind registrationHandler
      route "/auth"     >=> jsonBind authenticationHandler
    ]
  )
