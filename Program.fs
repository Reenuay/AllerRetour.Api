open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Server.Kestrel.Core
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe

let webApp =
  choose [
    route "/" >=> text "It works!"
    RequestErrors.NOT_FOUND "Not found"
  ]

type Startup(config : IConfiguration, env : IWebHostEnvironment) =
  member _.Configure(app : IApplicationBuilder) =
    match env.EnvironmentName with
    | "Development" -> app.UseDeveloperExceptionPage()
    | _             -> app
    |> ignore

    app.UseGiraffe webApp

  member _.ConfigureServices(services : IServiceCollection) =
    services
      .Configure<KestrelServerOptions>( config.GetSection("Kestrel") )
      .AddGiraffe() |> ignore

let buildWebHost (args : string array) =
  WebHostBuilder()
    .UseKestrel()
    .UseContentRoot(Directory.GetCurrentDirectory())
    .ConfigureAppConfiguration(
      fun ctx builder ->
        let env = ctx.HostingEnvironment

        builder
          .SetBasePath(env.ContentRootPath)
          .AddYamlFile("appsettings.yml")
          .AddYamlFile(sprintf "appsettings.%s.yml" env.EnvironmentName, true, true)
          .AddEnvironmentVariables()
          .AddCommandLine(args)
          |> ignore
    )
    .UseStartup<Startup>()
    .Build()

[<EntryPoint>]
let main args =
  (buildWebHost args).Run()
  0
