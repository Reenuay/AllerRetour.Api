open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe

let webApp =
  choose [
    route "/" >=> text "It works!"
  ]

let configureApp (app : IApplicationBuilder) =
  app.UseGiraffe webApp

let configureServices (services : IServiceCollection) =
  services.AddGiraffe() |> ignore

[<EntryPoint>]
let main _ =
  WebHostBuilder()
    .UseKestrel()
    .Configure(Action<IApplicationBuilder> configureApp)
    .ConfigureServices(configureServices)
    .Build()
    .Run()
  0
