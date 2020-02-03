module AllerRetour.Command

open Db
open RequestTypes

let createConfirmationToken email =
  let tokenString = Generators.randomGuid ()

  let token = emailConfirmationTokens.Create ()
  token.Email <- email
  token.TokenHash <- Pbkdf2.strongHash tokenString

  submit ()

  tokenString

let registerCustomer (input: SignUpRequest) =
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

let confirmEmail (customer: Customer, token: EmailConfirmationToken) =
  customer.EmailConfirmed <- true
  token.Delete ()
  submit()
