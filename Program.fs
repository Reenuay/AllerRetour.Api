open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Configuration
open Serilog
open Giraffe

let setHostConfig (basePath : string) (args : string array) (hostBuilder : IHostBuilder) =
  hostBuilder
    .UseContentRoot(basePath)
    .UseSerilog()
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
        let env = context.HostingEnvironment.EnvironmentName
        let rootPath = context.HostingEnvironment.ContentRootPath

        builder
          .SetBasePath(rootPath)
          .AddYamlFile("appsettings.yml")
          .AddYamlFile(sprintf "appsettings.%s.yml" env, true)
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
              let env = context.HostingEnvironment.EnvironmentName

              match env with
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

  Log.Logger <-
    LoggerConfiguration()
      .Enrich.FromLogContext()
      .WriteTo.Console()
      .WriteTo.File(
        "/var/log/aller_retour/api.log",
        fileSizeLimitBytes = Nullable(1000000L),
        rollOnFileSizeLimit = true,
        shared =  true,
        flushToDiskInterval = Nullable(TimeSpan.FromSeconds(1.0))
      )
      .CreateLogger()

  HostBuilder()
  |> setHostConfig basePath args
  |> setAppConfig
  |> setGiraffeAppConfig Router.app
  |> buildHost
  |> run

  0 // exit code
