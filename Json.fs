module AllerRetour.Json

open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.FSharpLu.Json

let serialize = Compact.Strict.serialize
let inline deserialize s = Compact.Strict.tryDeserialize s

let inline tryBind (parsingErrorHandler : HttpHandler)
                   (successHandler      : ^T -> HttpHandler) : HttpHandler =
  fun (next: HttpFunc) (ctx: HttpContext) ->
    task {
      let! payload = ctx.ReadBodyFromRequestAsync()
      let  res     = deserialize payload

      return!
        (match res with
        | Choice1Of2 dto -> successHandler dto
        | Choice2Of2 _   -> parsingErrorHandler) next ctx
    }
