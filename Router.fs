module Router

open Giraffe


[<CLIMutable>]
type CustomerCredentials = {
  Email: string
  Password: string
}

let register : HttpHandler
  = bindJson<CustomerCredentials> ( fun credentials ->
    text (sprintf "%s - %s" credentials.Email credentials.Password)
  )

let app : HttpHandler =
  subRoute "/customer" (
    choose [
      route "/register" >=> POST >=> register
    ]
  )
