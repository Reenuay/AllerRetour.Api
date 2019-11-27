module Router

open Giraffe

let app : HttpHandler =
  choose [
    route "/" >=> text "It works!"
    RequestErrors.NOT_FOUND "Not found"
  ]
