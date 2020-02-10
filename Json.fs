module AllerRetour.Json

open Microsoft.FSharpLu.Json

let serialize (x: obj) =
  match x with
  | :? string as s -> s
  | _ -> Compact.Strict.serialize x

let inline deserialize s = Compact.Strict.tryDeserialize s
