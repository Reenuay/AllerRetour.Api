module Queue

open Db

let customerIdByEmail email =
  query {
    for c in customers do
    where (c.Email = email)
    select (c.Id)
  }

let checkEmailAlreadyRegistered email = (customerIdByEmail email |> Seq.tryExactlyOne) <> None
