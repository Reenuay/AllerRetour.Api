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

let emailConfirmationTokenByEmail email =
  let now = DateTime.UtcNow

  query {
    for t in emailConfirmationTokens do
    where (
      t.Email = email
      && t.DateExpires > now
      && t.IsUsed = false
    )
    select t
  }

let emailConfirmationToken email token =
  query {
    for t in emailConfirmationTokenByEmail email do
    where (t.Token = token)
    select t
  }
