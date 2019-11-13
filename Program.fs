open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe

let webApp =
  choose [
    route "/" >=> text "It works!"
  ]

type Startup() =
  member _.Configure(app : IApplicationBuilder) =
    app.UseHttpsRedirection() |> ignore
    app.UseGiraffe webApp

  member _.ConfigureServices(services : IServiceCollection) =
    services.AddGiraffe() |> ignore

[<EntryPoint>]
let main _ =
  WebHostBuilder()
    .UseKestrel()
    .UseStartup<Startup>()
    .Build()
    .Run()
  0
