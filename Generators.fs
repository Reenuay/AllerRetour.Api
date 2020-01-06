module AllerRetour.Generators

open System

let private random = Random()

let randomInteger n = random.Next n

let randomCardId () =
  let chars  = [|'0' .. '9'|]
  let length = 16
  String(
    Array.init length (fun _ -> chars.[Array.length chars |> randomInteger])
  )

let randomGuid () = Guid.NewGuid().ToString()
