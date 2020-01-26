module AllerRetour.Output

open System

type AuthResponse = {
  Token: string
  EmailConfirmed: bool
  Expires: DateTime
}

type ProfileResponse = {
  Email: string
  CardId: string
  FirstName: string
  LastName: string
  Birthday: DateTime
  Gender: string
}
