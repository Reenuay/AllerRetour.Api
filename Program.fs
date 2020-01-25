module AllerRetour.Program

open System
open System.IO
open System.Text
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Hosting
open Microsoft.IdentityModel.Tokens
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.HttpOverrides
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
  tokenParams.ValidIssuer <- Globals.Auth.Issuer
  tokenParams.ValidAudience <- Globals.Auth.Audience
  tokenParams.IssuerSigningKey <- SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(Globals.Auth.Secret)
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

  Globals.Server.Host <- config.["Server:Host"]

  Globals.Auth.Secret   <- config.["Auth:Secret"]
  Globals.Auth.Issuer   <- config.["Auth:Issuer"]
  Globals.Auth.Audience <- config.["Auth:Audience"]

  Globals.Mail.Host    <- config.["Mail:Host"]
  Globals.Mail.Address <- config.["Mail:Address"]

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
        |> ignore

      services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(fun options ->
          options.TokenValidationParameters <- createTokenParams()
        )
        |> ignore

      services
        .AddAuthorization(fun options ->
           options
            .AddPolicy(
              Auth.mustHaveConfirmedEmailPolicy,
              Action<AuthorizationPolicyBuilder>(
                fun policy ->
                  policy.RequireClaim(Auth.emailConfirmedClaim)
                  |> ignore
              )
            )
        )
        |> ignore

      services
        .Configure<ForwardedHeadersOptions>(
          Action<ForwardedHeadersOptions>(
            fun options ->
              options.ForwardedHeaders <-
                ForwardedHeaders.XForwardedFor ||| ForwardedHeaders.XForwardedProto
          )
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
                .UseForwardedHeaders()
                .UseAuthentication()
                .UseGiraffe (createApp())
          )
          |> ignore
    )
    .Build()
    .Run()

  0 // exit code
