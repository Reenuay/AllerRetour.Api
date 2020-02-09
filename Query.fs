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
  query {
    for t in emailConfirmationTokens do
    where (t.Email = email)
    select t
  }

let unexpiredEmailConfirmationToken email =
  let now = DateTime.UtcNow

  query {
    for t in emailConfirmationToken email do
    where (t.DateExpires > now)
    select t
  }

let passwordResetToken email =
  query {
    for t in passwordResetTokens do
    where (t.Email = email)
    select t
  }

let unexpiredPasswordResetToken email =
  let now = DateTime.UtcNow

  query {
    for t in passwordResetToken email do
    where (t.DateExpires > now)
    select t
  }
