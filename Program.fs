open System
open System.IO
open System.Text
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Hosting
open Microsoft.IdentityModel.Tokens
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Authentication.JwtBearer

open Serilog
open Giraffe

open Router

// TO DO: Refactor all this staff!!
let secret = "I'm just a stub! Put me into a config file!"

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

              let tokenParams = TokenValidationParameters()
              tokenParams.ValidateIssuer <- true
              tokenParams.ValidateAudience <- true
              tokenParams.ValidateLifetime <- true
              tokenParams.ValidateIssuerSigningKey <- true
              tokenParams.ValidIssuer <- "aller-retour.com"
              tokenParams.ValidAudience <- "aller-retour.com"
              tokenParams.IssuerSigningKey <- SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))

              services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(fun options ->
                  options.TokenValidationParameters <- tokenParams
                )
                |> ignore
          )
          .Configure(
            fun context app ->
              let env = context.HostingEnvironment.EnvironmentName

              match env with
              | "Development" -> app.UseDeveloperExceptionPage()
              | _             -> app
              |> ignore

              app
                .UseAuthentication()
                .UseGiraffe giraffeApp
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

  let appSettings = {
    Auth = {
      Secret = secret
      Issuer = "aller-retour.com"
      Audience = "aller-retour.com"
    }
  }

  HostBuilder()
  |> setHostConfig basePath args
  |> setAppConfig
  |> setGiraffeAppConfig (createApp appSettings)
  |> buildHost
  |> run

  0 // exit code
