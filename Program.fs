open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Configuration

open Giraffe

let webApp =
  choose [
    route "/" >=> text "It works!"
    RequestErrors.NOT_FOUND "Not found"
  ]

let setHostConfig (basePath : string) (args : string array) (hostBuilder : IHostBuilder) =
  hostBuilder
    .UseContentRoot(basePath)
    .ConfigureHostConfiguration(
      fun builder ->
        builder
          .SetBasePath(basePath)
          .AddYamlFile("hostsettings.yml")
          .AddEnvironmentVariables("DOTNET_")
          .AddCommandLine(args)
          |> ignore
    )

let setAppConfig (hostBuilder : IHostBuilder) =
  hostBuilder
    .ConfigureAppConfiguration(
      fun context builder ->
        let env = context.HostingEnvironment

        builder
          .SetBasePath(env.ContentRootPath)
          .AddYamlFile("appsettings.yml")
          .AddYamlFile(sprintf "appsettings.%s.yml" env.EnvironmentName, true)
          .AddEnvironmentVariables("ASPNETCORE_")
          |> ignore
    )

let setGiraffeAppConfig (giraffeApp : HttpHandler ) (hostBuilder : IHostBuilder) =
  hostBuilder
    .ConfigureWebHost(
      fun webBuilder ->
        webBuilder
          .UseKestrel(
            fun context options ->
              options.Configure(
                context.Configuration.GetSection("Kestrel")
              )
              |> ignore
          )
          .ConfigureServices(
            fun services ->
              services.AddGiraffe() |> ignore
          )
          .Configure(
            fun context app ->
              let env = context.HostingEnvironment

              match env.EnvironmentName with
              | "Development" -> app.UseDeveloperExceptionPage()
              | _             -> app
              |> ignore

              app.UseGiraffe giraffeApp
          )
          |> ignore
    )

let buildHost (hostBuilder : IHostBuilder) = hostBuilder.Build()

let run (host : IHost) = host.Run()

[<EntryPoint>]
let main args =
  let basePath = Directory.GetCurrentDirectory()

  HostBuilder()
  |> setHostConfig basePath args
  |> setAppConfig
  |> setGiraffeAppConfig webApp
  |> buildHost
  |> run

  0 // exit code
