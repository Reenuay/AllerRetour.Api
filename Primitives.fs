[<AutoOpen>]
module AllerRetour.Primitives
  let tee f x =
    f x |> ignore
    x

  let tryCatch fSuccess fError f x =
    try
      x
      |> f
      |> fSuccess
    with
    | ex -> fError ex.Message

  let ignore2 x _ = x

  module List =
    let wrap x = [x]
