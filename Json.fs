module AllerRetour.Json

open Microsoft.FSharpLu.Json

let serialize = Compact.Strict.serialize
let inline deserialize s = Compact.Strict.tryDeserialize s
