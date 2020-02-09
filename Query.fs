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
let emailConfirmationToken customerId =
  query {
    for t in emailConfirmationTokens do
    where (t.CustomerId = customerId)
    select t
  }

let unexpiredEmailConfirmationToken customerId =
  let now = DateTime.UtcNow

  query {
    for t in emailConfirmationToken customerId do
    where (t.DateExpires > now)
    select t
  }

let passwordResetToken customerId =
  query {
    for t in passwordResetTokens do
    where (t.CustomerId = customerId)
    select t
  }

let unexpiredPasswordResetToken customerId =
  let now = DateTime.UtcNow

  query {
    for t in passwordResetToken customerId do
    where (t.DateExpires > now)
    select t
  }
