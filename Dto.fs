module Dto

open ResultBuilder
open Validator
open Db

module RegistrationRequest =
  type T = {
    FirstName: string
    LastName: string
    Email: string
    Password: string
  }

  let private passwordField = "Password"
  let private passwordMinLength = 8
  let private passwordMaxLength = 300

  let private passwordValidator
    = checkMinLength passwordField passwordMinLength
    ++ checkMaxLength passwordField passwordMaxLength
    ++ checkHasDigits passwordField
    ++ checkHasLetters passwordField
    ++ checkHasSymbols passwordField

  let private nameMinLength = 1
  let private nameMaxLength = 100

  let private nameValidator field
    = checkMinLength field nameMinLength
    ++ checkMaxLength field nameMaxLength

  let validateEmail r =
    match checkIsEmail "Email" r.Email with
    | Ok _    -> Ok r
    | Error e -> Error e

  let validatePassword r =
    match passwordValidator r.Password with
    | Ok _    -> Ok r
    | Error e -> Error e

  let validateFirstName r =
    match nameValidator "First name" r.FirstName with
    | Ok _    -> Ok r
    | Error e -> Error e

  let validateLastName r =
    match nameValidator "Last name" r.LastName with
    | Ok _    -> Ok r
    | Error e -> Error e

  let validate
    = validateEmail
    ++ validatePassword
    ++ validateFirstName
    ++ validateLastName

  let cleanName r = {
    r with
      FirstName = Validator.cleanWhiteSpace r.FirstName
      LastName  = Validator.cleanWhiteSpace r.LastName
  }

module CustomerResponse =
  type T = {
    Id: int64
    FirstName: string
    LastName: string
    Email: string
    CardId: string
  }

  let fromDb
    (c: AllerRetourSchema.dataContext.``public.customersEntity``)
    (p: AllerRetourSchema.dataContext.``public.customer_profilesEntity``) =
    {
      Id = c.Id
      FirstName = p.FirstName
      LastName = p.LastName
      Email = c.Email
      CardId = c.CardId
    }
