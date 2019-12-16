module Dto

open ResultUtils
open Validator
open Cleaner
open Db

module DtoValidator =
  let emailError field = sprintf "%s has bad email format" field
  let minLengthError field l
    = sprintf "%s must be at least %i characters long" field l
  let maxLengthError field l
    = sprintf "%s can be maximum %i characters long" field l
  let restrictedWordsError field
    = sprintf "%s is not allowed to contain any part of application name" field

  module private Pass =
    let min = 8
    let max = 300
    let words = ["aller"; "retour"]

  module private Name =
    let min = 1
    let max = 100

  let emailValidator field = chain isEmail ([emailError field])

  let passwordValidator field = chainList [
    hasMinLengthOf Pass.min, minLengthError field Pass.min
    hasMaxLengthOf Pass.max, maxLengthError field Pass.max
    containsWords Pass.words >> not, restrictedWordsError field
  ]

  let nameValidator field = chainList [
    hasMinLengthOf Name.min, minLengthError field Name.min
    hasMaxLengthOf Name.max, maxLengthError field Name.max
  ]

module RegistrationRequest =

  open DtoValidator

  type T = {
    FirstName: string
    LastName: string
    Email: string
    Password: string
  }

  let validateFirstName = adapt (nameValidator "First name") (fun r -> r.FirstName)
  let validateLastName  = adapt (nameValidator "Last name") (fun r -> r.LastName)
  let validateEmail     = adapt (emailValidator "Email") (fun r -> r.Email)
  let validatePassword  = adapt (passwordValidator "Password") (fun r -> r.Password)
  let validate
    =  validateFirstName
    ++ validateLastName
    ++ validateEmail
    ++ validatePassword

  let cleanName r = {
    r with
      FirstName = cleanWhiteSpace r.FirstName
      LastName  = cleanWhiteSpace r.LastName
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
