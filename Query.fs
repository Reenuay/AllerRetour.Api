module AllerRetour.Query

open Db

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
