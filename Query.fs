module AllerRetour.Query

open System
open Db

let customerByEmail email =
  query {
    for c in customers do
    where (c.Email = email)
    select c
  }

let emailConfirmationToken email token =
  let nowPlusMinute = DateTime.UtcNow.AddMinutes(1.0)

  query {
    for t in emailConfirmationTokens do
    where (
      t.Email = email
      && t.Token = token
      && t.DateExpires > nowPlusMinute
      && t.IsUsed = false
    )
    select t
  }
