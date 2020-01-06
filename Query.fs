module AllerRetour.Query

open Db
open Dto

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
