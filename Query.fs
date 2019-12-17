module Query

open Db
open Dto.Customer

let customerByEmail email =
  query {
    for c in customers do
    where (c.Email = email)
    select ({
      Id = c.Id
      Email = c.Email
      PasswordHash = c.PasswordHash
    })
  }

let checkEmailAlreadyRegistered email = (customerByEmail email |> Seq.tryExactlyOne) <> None
