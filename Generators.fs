module AllerRetour.Generators

open System

let private random = Random()

let randomInteger n = random.Next n

let randomNumberId length =
  let chars  = [|'0' .. '9'|]
  String(
    Array.init length (fun _ -> chars.[Array.length chars |> randomInteger])
  )

let randomCardId () = randomNumberId 16

let randomPin () = randomNumberId 6

let randomGuid () = Guid.NewGuid().ToString()
