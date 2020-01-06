module AllerRetour.Status

open Giraffe

let jsonWithCode code x = setStatusCode code >=> text (Json.serialize x)

let ok x = jsonWithCode 200 x
let validationError x = jsonWithCode 400 x
let unauthorizedError x = jsonWithCode 401 x
let notFoundError x = jsonWithCode 404 x
let conflictError x = jsonWithCode 409 x
let serverError : HttpHandler = jsonWithCode 500 "Server error"
