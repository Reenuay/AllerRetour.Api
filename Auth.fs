module AllerRetour.Auth

open System
open System.Text
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt
open Microsoft.IdentityModel.Tokens

let tokenExpirationTime = 15.0
let mustHaveConfirmedEmailPolicy = "MustHaveConfirmedEmail"

let customerIdClaim = "AllerRetour.CustomerId"
let emailClaim = "AllerRetour.Email"
let emailConfirmedClaim = "AllerRetour.EmailConfirmed"

let generateToken (id: int64) (emailConfirmed: bool) email =
  let claims = [|
    yield Claim(JwtRegisteredClaimNames.Sub, id.ToString())
    yield Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    yield Claim(customerIdClaim, id.ToString())
    yield Claim(emailClaim, email)
    yield!
      if emailConfirmed then
        [| Claim(emailConfirmedClaim, "confirmed") |]
      else
      [||]
  |]

  let expires = DateTime.UtcNow.AddMinutes(tokenExpirationTime)
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
      expires = Nullable(expires),
      notBefore = notBefore,
      signingCredentials = signingCredentials
    )

  JwtSecurityTokenHandler().WriteToken(token), expires
