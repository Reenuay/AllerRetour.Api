module AllerRetour.Dto

open Db

type Customer = {
  Id: int64
  Email: string
  PasswordHash: string
}

module Customer =
  let fromDb (c: AllerRetourSchema.dataContext.``public.customersEntity``) = {
    Id = c.Id
    Email = c.Email
    PasswordHash = c.PasswordHash
  }
