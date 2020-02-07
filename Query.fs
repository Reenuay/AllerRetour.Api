module AllerRetour.Query

open System
open Db

let customerByEmail email =
  query {
    for c in customers do
    where (c.Email = email)
    select c
  }

let customerById id =
  query {
    for c in customers do
    where (c.Id = id)
    select c
  }
let emailConfirmationToken email =
  let now = DateTime.UtcNow

  query {
    for t in emailConfirmationTokens do
    where (
      t.Email = email
      && t.DateExpires > now
    )
    select t
  }
