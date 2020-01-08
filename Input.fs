module AllerRetour.Input

open Validators
open Normalizers
open TwoTrackResult

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
  let guidError field = [sprintf "%s has bad format" field]

  module private Email =
    let max = 100

  module private Pass =
    let min = 8
    let max = 100
    let words = ["aller"; "retour"]

  module private Name =
    let min = 1
    let max = 100

  module private Guid =
    let length = 36

  let emailValidator field
    =  chain isValidEmail (emailError field)
    ++ chain (hasMaxLengthOf Email.max) (maxLengthError field Email.max)

  let passwordValidator field
    =  chain (hasMinLengthOf Pass.min) (minLengthError field Pass.min)
    ++ chain (hasMaxLengthOf Pass.max) (maxLengthError field Pass.max)
    ++ chain (containsWords Pass.words >> not) (restrictedWordsError field)

  let nameValidator field
    =  chain (hasMinLengthOf Name.min) (minLengthError field Name.min)
    ++ chain (hasMaxLengthOf Name.max) (maxLengthError field Name.max)

  let guidValidator field
    =  chain isValidGuid (guidError field)
    ++ chain (hasExactLengthOf Guid.length) (exactLengthError field Guid.length)

module RegRequest =

  open GenericValidators

  type T = {
    FirstName: string
    LastName: string
    Email: string
    Password: string
  }

  let private cleanName r = {
    r with
      FirstName = cleanWhiteSpace r.FirstName
      LastName  = cleanWhiteSpace r.LastName
  }

  let validate
    =  adapt (nameValidator "First name") (fun r -> r.FirstName)
    ++ adapt (nameValidator "Last name") (fun r -> r.LastName)
    ++ adapt (emailValidator "Email") (fun r -> r.Email)
    ++ adapt (passwordValidator "Password") (fun r -> r.Password)
    >> map cleanName
    >> either succeed (Validation >> fail)

module AuthRequest =

  open GenericValidators

  type T = {
    Email: string
    Password: string
  }

  let validate
    =  adapt (emailValidator "Email") (fun a -> a.Email)
    ++ adapt (passwordValidator "Password") (fun a -> a.Password)
    >> either succeed (Validation >> fail)

module EmailConfirmRequest =

  open GenericValidators

  type T = {
    Email: string
    Code: string
  }

  let validate
    =  adapt (emailValidator "Email") (fun a -> a.Email)
    ++ adapt (guidValidator "Code") (fun a -> a.Code)
    >> either succeed (Validation >> fail)
