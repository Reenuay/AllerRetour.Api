module AllerRetour.Auth

open System
open System.Text
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt
open Microsoft.IdentityModel.Tokens

let mustHaveConfirmedEmailPolicy = "MustHaveConfirmedEmail"
let customerIdClaim = "customerId"
let emailConfirmedClaim = "emailConfirmed"

let generateToken (id: int64) (emailConfirmed: bool) email =
  let claims = [|
    yield Claim(customerIdClaim, id.ToString())
    yield Claim(JwtRegisteredClaimNames.Sub, email)
    yield Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    yield!
      if emailConfirmed then
        [| Claim(emailConfirmedClaim, "confirmed") |]
      else
      [||]
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

  JwtSecurityTokenHandler().WriteToken(token)
