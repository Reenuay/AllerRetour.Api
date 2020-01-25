module AllerRetour.Output

open System

type AuthResponse = {
  Token: string
  EmailConfirmed: bool
  Expires: DateTime
}
