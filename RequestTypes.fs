module AllerRetour.RequestTypes

open System
open Validators
open Normalizers
open TwoTrackResult

[<CLIMutable>]
type SignInRequest = {
  Email: string
  Password: string
}

[<CLIMutable>]
type SignUpRequest = {
  FirstName: string
  LastName: string
  Email: string
  Password: string
}

[<CLIMutable>]
type EmailTokenRequest = {
  Email: string
  Token: string
}

[<CLIMutable>]
type PasswordResetRequest = {
  Email: string
}

[<CLIMutable>]
type UpdateProfileRequest = {
  FirstName: string
  LastName: string
  Birthday: DateTime option
  Gender: string
}

[<CLIMutable>]
type ChangeEmailRequest = {
  NewEmail: string
  Password: string
}

[<CLIMutable>]
type ChangePasswordRequest = {
  NewPassword: string
  OldPassword: string
}

[<CLIMutable>]
type CustomerIdentity = {
  Id: int64
  Email: string
}

module private GenericValidators =
  let emailError field = [sprintf "%s has bad email format" field]
  let minLengthError field l
    = [sprintf "%s must be at least %i characters long" field l]
  let maxLengthError field l
    = [sprintf "%s can be maximum %i characters long" field l]
  let exactLengthError field l
    = [sprintf "%s must be exactly %i characters long" field l]
  let restrictedWordsError field
    = [sprintf "%s is not allowed to contain any part of application name" field]
  let isNot120YearsAgoError =
    ["Birthday must be maximum 120 years ago"]
  let isAtLeast18YearsAgoError =
    ["You must be at least 18 years old"]

  let emailValidator field =
    let max = 100

    chain isValidEmail (emailError field)
    ++ chain (hasMaxLengthOf max) (maxLengthError field max)

  let passwordValidator field =
    let min = 8
    let max = 100
    let words = ["aller"; "retour"]

    chain (hasMinLengthOf min) (minLengthError field min)
    ++ chain (hasMaxLengthOf max) (maxLengthError field max)
    ++ chain (containsWords words >> not) (restrictedWordsError field)

  let nameValidator field =
    let min = 1
    let max = 100

    chain (hasMinLengthOf min) (minLengthError field min)
    ++ chain (hasMaxLengthOf max) (maxLengthError field max)

  let birthdayValidator = function
  | Some b ->
    b
    |> (chain isNot120YearsAgo isNot120YearsAgoError
    ++ chain isAtLeast18YearsAgo isAtLeast18YearsAgoError
    >> bind (Some >> succeed))

  | None ->
    succeed None

  let genderValidator =
    let max = 100

    chain (hasMaxLengthOf max) (maxLengthError "Gender" max)

  let toValidation x = either succeed (Validation >> fail) x

module SignInRequest =

  open GenericValidators

  let validate
    =  adapt (emailValidator "Email") (fun (r: SignInRequest) -> r.Email)
    ++ adapt (passwordValidator "Password") (fun r -> r.Password)
    >> toValidation

module SignUpRequest =

  open GenericValidators

  let private cleanName (r: SignUpRequest) = {
    r with
      FirstName = cleanWhiteSpace r.FirstName
      LastName  = cleanWhiteSpace r.LastName
  }

  let validate
    =  adapt (nameValidator "First name") (fun (r: SignUpRequest) -> r.FirstName)
    ++ adapt (nameValidator "Last name") (fun r -> r.LastName)
    ++ adapt (emailValidator "Email") (fun r -> r.Email)
    ++ adapt (passwordValidator "Password") (fun r -> r.Password)
    >> map cleanName
    >> toValidation

module EmailTokenRequest =

  open GenericValidators

  let validate
    =  adapt (emailValidator "Email") (fun (r: EmailTokenRequest) -> r.Email)
    >> toValidation

module PasswordResetRequest =

  open GenericValidators

  let validate
    =  adapt (emailValidator "Email") (fun (r: PasswordResetRequest) -> r.Email)
    >> toValidation

module UpdateProfileRequest =

  open GenericValidators

  let private cleanName (r: UpdateProfileRequest) = {
    r with
      FirstName = cleanWhiteSpace r.FirstName
      LastName  = cleanWhiteSpace r.LastName
  }

  let validate
    =  adapt (nameValidator "First name") (fun (r: UpdateProfileRequest) -> r.FirstName)
    ++ adapt (nameValidator "Last name") (fun r -> r.LastName)
    ++ adapt (birthdayValidator) (fun r -> r.Birthday)
    ++ adapt genderValidator (fun r -> r.Gender)
    >> map cleanName
    >> toValidation

module ChangeEmailRequest =

  open GenericValidators

  let validate
    =  adapt (emailValidator "New email") (fun r -> r.NewEmail)
    ++ adapt (passwordValidator "Password") (fun r -> r.Password)
    >> toValidation

module ChangePasswordRequest =

  open GenericValidators

  let validate
    =  adapt (passwordValidator "NewPassword") (fun r -> r.NewPassword)
    ++ adapt (passwordValidator "OldPassword") (fun r -> r.OldPassword)
    >> toValidation
