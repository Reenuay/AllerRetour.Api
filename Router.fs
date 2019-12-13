module Router

open Giraffe
open Result
open ResultBuilder
open Dto

let onError = function
| Validation e -> Json.serialize e |> RequestErrors.BAD_REQUEST
| Conflict   e -> Json.serialize e |> RequestErrors.CONFLICT
| Fatal      e ->
  printfn "%A" e // TO DO: Add Logs!
  ServerErrors.INTERNAL_ERROR "Oops! Something went wrong..."

let toHandler = map2 Successful.OK onError

let register input =
  warbler (fun _ ->
    result {
      let! valid
        = Registration.validate input
        |> map Registration.trimName
        |> toValidationError

      do! Queue.checkEmailAlreadyRegistered valid.Email
        |> errorIfTrue (Conflict ["Email is already registered"])

      return! (tryCatch Command.registerCustomer valid) |> toFatalError
    }
    |> toHandler
  )

let badRequest _ = RequestErrors.BAD_REQUEST "Bad request"

let app : HttpHandler =
  subRoute "/customer" (
    choose [
      route "/register" >=> POST >=> Json.tryBind badRequest register
    ]
  )
