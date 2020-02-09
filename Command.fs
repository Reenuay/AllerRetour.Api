module AllerRetour.Command

open Db
open RequestTypes

let hash = Pbkdf2.strongHash

let createConfirmationToken customerId =
  let tokenString = Generators.randomGuid ()

  let token = passwordResetTokens.Create ()
  token.CustomerId <- customerId
  token.TokenHash  <- hash tokenString

  submit ()

  tokenString

let createResetToken customerId =
  let tokenString = Generators.randomPin ()

  let token = emailConfirmationTokens.Create ()
  token.CustomerId <- customerId
  token.TokenHash  <- hash tokenString

  submit ()

  tokenString

let changePassword (customer: Customer) password =
  customer.PasswordHash <- hash password

  submit ()

let registerCustomer (request: SignUpRequest) =
  let cardId = Generators.randomCardId ()

  let customer = customers.Create ()
  customer.Email        <- request.Email
  customer.CardId       <- cardId
  customer.PasswordHash <- hash request.Password

  submit ()

  let profile = customerProfiles.Create ()
  profile.CustomerId <- customer.Id
  profile.FirstName  <- request.FirstName
  profile.LastName   <- request.LastName

  submit ()

  customer

let confirmEmail (customer: Customer) (token: EmailConfirmationToken) =
  customer.EmailConfirmed <- true
  token.Delete ()

  submit()

let updateProfile (profile: Profile) (request: UpdateProfileRequest) =
  profile.FirstName <- request.FirstName
  profile.LastName <- request.LastName
  profile.Birthday <- request.Birthday
  profile.Gender <- request.Gender

  submit ()

let changeEmail  (customer: Customer) newEmail =
  customer.Email          <- newEmail
  customer.EmailConfirmed <- false

  submit()
