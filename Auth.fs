module AllerRetour.Auth

open System
open System.Text
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt
open Microsoft.IdentityModel.Tokens

open Dto

[<AbstractClass; Sealed>]
type Settings private () =
  static member val Secret = "" with get, set
  static member val Issuer = "" with get, set
  static member val Audience = "" with get, set

type TokenResult = {
  Token : string
}

let customerIdClaim = "customerId"

let generateToken customer =
  let claims = [|
    Claim(customerIdClaim, customer.Id.ToString())
    Claim(JwtRegisteredClaimNames.Sub, customer.Email)
    Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
  |]

  let expires = Nullable(DateTime.UtcNow.AddMinutes(15.0))
  let notBefore = Nullable(DateTime.UtcNow)
  let securityKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(Settings.Secret))
  let signingCredentials =
    SigningCredentials(
      key = securityKey,
      algorithm = SecurityAlgorithms.HmacSha256
    )

  let token =
    JwtSecurityToken(
      issuer = Settings.Issuer,
      audience = Settings.Audience,
      claims = claims,
      expires = expires,
      notBefore = notBefore,
      signingCredentials = signingCredentials
    )

  let tokenResult = {
    Token = JwtSecurityTokenHandler().WriteToken(token)
  }

  tokenResult
