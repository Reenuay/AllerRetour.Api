module Router

open Giraffe
open ResultUtils
open Dto

let jsonWithCode code x = setStatusCode code >=> text (Json.serialize x)

let onError = function
| Validation e -> jsonWithCode 400 e
| NotFound   e -> jsonWithCode 404 e
| Conflict   e -> jsonWithCode 409 e
| Fatal      e ->
  printfn "%A" e // TO DO: Add Logs!
  jsonWithCode 500 "Oops! Something went wrong..."

let toHandler = function
| Ok    o -> jsonWithCode 200 o
| Error e -> onError e

let registrationHandler input =
  warbler (fun _ ->
    result {
      let! valid
        = RegistrationRequest.validate input
        |> Result.map RegistrationRequest.cleanName
        |> toValidationError

      do! Query.checkEmailAlreadyRegistered valid.Email
        |> not
        |> errorIf ["Email is already registered"]
        |> toConflictError

      return! (tryCatch Command.registerCustomer valid) |> toFatalError
    }
    |> toHandler
  )

let authenticationHandler input =
  warbler (fun _ ->
    result {
      let! valid =
        AuthenticationRequest.validate input
        |> toValidationError

      let! customer =
        Query.customerByEmail valid.Email
        |> Seq.tryExactlyOne
        |> fromOption ["User with that email is not found"]
        |> toNotFoundError

      do! Pbkdf2.verify customer.PasswordHash input.Password
        |> errorIf ["Invalid password"]
        |> toValidationError

      return "Heysan"
    }
    |> toHandler
  )

let badRequest _ = RequestErrors.BAD_REQUEST "Bad request"

let inline jsonBind (handler : ^T -> HttpHandler) = Json.tryBind badRequest handler

let app : HttpHandler =
  subRoute "/customer" (
    choose [
      route "/register" >=> POST >=> jsonBind registrationHandler
      route "/auth" >=> POST >=> jsonBind authenticationHandler
    ]
  )
