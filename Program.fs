open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection

open Giraffe

let webApp =
  choose [
    route "/" >=> text "It works!"
    RequestErrors.NOT_FOUND "Not found"
  ]

let buildWebHost (args : string array) =
  HostBuilder()
    .UseContentRoot(Directory.GetCurrentDirectory())
    .ConfigureHostConfiguration(
      fun builder ->
        builder
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddYamlFile("hostsettings.yml")
          .AddEnvironmentVariables("DOTNET_")
          .AddCommandLine(args)
          |> ignore
    )
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
    .ConfigureWebHostDefaults(
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

              app.UseGiraffe webApp
          )
          |> ignore
    )
    .Build()

[<EntryPoint>]
let main args =
  (buildWebHost args).Run()
  0
