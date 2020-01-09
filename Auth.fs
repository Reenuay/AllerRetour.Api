module AllerRetour.Auth

open System
open System.Text
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt
open Microsoft.IdentityModel.Tokens

type TokenResult = {
  Token : string
}

let customerIdClaim = "customerId"

let generateToken (id: int64) email =
  let claims = [|
    Claim(customerIdClaim, id.ToString())
    Claim(JwtRegisteredClaimNames.Sub, email)
    Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
  |]

  let expires = Nullable(DateTime.UtcNow.AddMinutes(15.0))
  let notBefore = Nullable(DateTime.UtcNow)
  let securityKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(Globals.Auth.Secret))
  let signingCredentials =
    SigningCredentials(
      key = securityKey,
      algorithm = SecurityAlgorithms.HmacSha256
    )

  let token =
    JwtSecurityToken(
      issuer = Globals.Auth.Issuer,
      audience = Globals.Auth.Audience,
      claims = claims,
      expires = expires,
      notBefore = notBefore,
      signingCredentials = signingCredentials
    )

  let tokenResult = {
    Token = JwtSecurityTokenHandler().WriteToken(token)
  }

  tokenResult
