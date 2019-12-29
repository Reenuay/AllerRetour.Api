module AllerRetour.Program

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

let createTokenParams () =
  let tokenParams = TokenValidationParameters()
  tokenParams.ValidateIssuer <- true
  tokenParams.ValidateAudience <- true
  tokenParams.ValidateLifetime <- true
  tokenParams.ValidateIssuerSigningKey <- true
  tokenParams.ValidIssuer <- Auth.Settings.Issuer
  tokenParams.ValidAudience <- Auth.Settings.Audience
  tokenParams.IssuerSigningKey <- SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(Auth.Settings.Secret)
  )
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

  Auth.Settings.Secret   <- config.["Auth:Secret"]
  Auth.Settings.Issuer   <- config.["Auth:Issuer"]
  Auth.Settings.Audience <- config.["Auth:Audience"]

  Mail.Settings.Host    <- config.["Mail:Host"]
  Mail.Settings.Address <- config.["Mail:Address"]

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

  HostBuilder()
    .UseContentRoot(basePath)
    .UseSerilog()
    .ConfigureServices(fun _ services ->
      services
        .AddGiraffe()
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(fun options ->
          options.TokenValidationParameters <- createTokenParams()
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
                .UseGiraffe (createApp())
          )
          |> ignore
    )
    .Build()
    .Run()

  0 // exit code
