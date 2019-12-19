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

open Auth
open Router

let createTokenParams settings =
  let tokenParams = TokenValidationParameters()
  tokenParams.ValidateIssuer <- true
  tokenParams.ValidateAudience <- true
  tokenParams.ValidateLifetime <- true
  tokenParams.ValidateIssuerSigningKey <- true
  tokenParams.ValidIssuer <- settings.Issuer
  tokenParams.ValidAudience <- settings.Audience
  tokenParams.IssuerSigningKey <- SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret))
  tokenParams

[<EntryPoint>]
let main _ =
  let basePath = Directory.GetCurrentDirectory()

  let config =
    ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddYamlFile("appsettings.yml")
      .AddYamlFile("appsettings.optional.yml", true)
      .Build()

  let logger =
    LoggerConfiguration()
      .Enrich.FromLogContext()
      .WriteTo.Console()
      .WriteTo.File(
        config.["Logger:Path"],
        shared = true,
        rollOnFileSizeLimit = true,
        fileSizeLimitBytes = Nullable(1000000L),
        flushToDiskInterval = Nullable(TimeSpan.FromSeconds(1.0))
      )
      .CreateLogger()

  Log.Logger <- logger

  let log = function
  | VerboseLevel -> logger.Verbose
  | DebugLevel   -> logger.Debug
  | InfoLevel    -> logger.Information
  | WarningLevel -> logger.Warning
  | ErrorLevel   -> logger.Error
  | FatalLevel   -> logger.Fatal

  let appSettings = {
    Auth = {
      Secret = config.["Auth:Secret"]
      Issuer = config.["Auth:Issuer"]
      Audience = config.["Auth:Audience"]
    }

    Log = log
  }

  HostBuilder()
    .UseContentRoot(basePath)
    .UseSerilog()
    .ConfigureServices(fun _ services ->
      services
        .AddGiraffe()
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(fun options ->
          options.TokenValidationParameters <- createTokenParams appSettings.Auth
        )
        |> ignore
    )
    .ConfigureWebHost(
      fun webBuilder ->
        webBuilder
          .UseKestrel()
          .Configure(
            fun ctx appBuilder ->
              let env = ctx.HostingEnvironment.EnvironmentName

              (
                match env with
                | "Development" -> appBuilder.UseDeveloperExceptionPage()
                | _             -> appBuilder
              )
                .UseAuthentication()
                .UseGiraffe (createApp appSettings)
          )
          |> ignore
    )
    .Build()
    .Run()

  0 // exit code
