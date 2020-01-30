module AllerRetour.ResponseTypes

open System

type SignInResponse = {
  Token: string
  EmailConfirmed: bool
  Expires: DateTime
}

type ProfileResponse = {
  Email: string
  CardId: string
  FirstName: string
  LastName: string
  Birthday: DateTime option
  Gender: string
}
