module Dto

open ResultBuilder
open Db

module RegistrationRequest =
  type T = {
    FirstName: string
    LastName: string
    Email: string
    Password: string
  }

  let validateEmail r =
    match Validators.email r.Email with
    | Ok _    -> Ok r
    | Error e -> Error e

  let validatePassword r =
    match Validators.password r.Password with
    | Ok _    -> Ok r
    | Error e -> Error e

  let validateFirstName r =
    match Validators.name r.FirstName "First name" with
    | Ok _    -> Ok r
    | Error e -> Error e

  let validateLastName r =
    match Validators.name r.LastName "Last name" with
    | Ok _    -> Ok r
    | Error e -> Error e

  let validate =
    validateEmail
    ++ validatePassword
    ++ validateFirstName
    ++ validateLastName

  let trimName r = {
    r with
      FirstName = Validators.trimName r.FirstName
      LastName  = Validators.trimName r.LastName
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
