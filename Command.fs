module AllerRetour.Command

open Db
open Input

let createConfirmationToken email =
  let guid = Generators.randomGuid ()

  let token = emailConfirmationTokens.Create ()
  token.Email <- email
  token.Token <- guid

  submit ()

  token

let registerCustomer (input: RegRequest.T) =
  let cardId = Generators.randomCardId ()
  let hash   = Pbkdf2.strongHash input.Password

  let customer = customers.Create ()
  customer.Email        <- input.Email
  customer.CardId       <- cardId
  customer.PasswordHash <- hash

  submit ()

  let profile = customerProfiles.Create ()
  profile.CustomerId <- customer.Id
  profile.FirstName  <- input.FirstName
  profile.LastName   <- input.LastName

  submit ()

  customer

let confirmEmail (customer: Customer) (token: EmailConfirmationToken) =
  customer.EmailConfirmed <- true
  token.IsUsed <- true
  submit()
