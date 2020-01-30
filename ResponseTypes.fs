module AllerRetour.ResponseTypes

open System

[<CLIMutable>]
type SignInResponse = {
  Token: string
  EmailConfirmed: bool
  Expires: DateTime
}

[<CLIMutable>]
type ProfileResponse = {
  Email: string
  CardId: string
  FirstName: string
  LastName: string
  Birthday: DateTime option
  Gender: string
}
