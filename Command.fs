module AllerRetour.Command

open Dto
open Input

let registerCustomer (input: RegRequest.T) =
  let cardId = Generators.randomCardId ()
  let hash   = Pbkdf2.strongHash input.Password

  let customer = Db.customers.Create ()
  customer.Email        <- input.Email
  customer.CardId       <- cardId
  customer.PasswordHash <- hash

  Db.submit ()

  let profile = Db.customerProfiles.Create ()
  profile.CustomerId <- customer.Id
  profile.FirstName  <- input.FirstName
  profile.LastName   <- input.LastName

  Db.submit ()

  Customer.fromDb customer
