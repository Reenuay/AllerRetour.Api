module Router

open Giraffe

let app : HttpHandler =
  subRoute "/customer" (
    choose [
      route "/register" >=> POST >=> Json.tryBind RequestErrors.BAD_REQUEST text
    ]
  )
