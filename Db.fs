module AllerRetour.Db

open FSharp.Data.Sql

let [<Literal>] DbVendor = Common.DatabaseProviderTypes.POSTGRESQL
let [<Literal>] ConnString = "Host=127.0.0.1;Port=5432;Database=aller_retour;Username=aller_retour"
let [<Literal>] UseOptionTypes = true
let [<Literal>] ResolutionPath = @"../.nuget/packages/npgsql/4.1.2/lib/netcoreapp3.0"

type AllerRetourSchema =
  SqlDataProvider<
    DbVendor,
    ConnString,
    UseOptionTypes = UseOptionTypes,
    ResolutionPath = ResolutionPath
  >

type Customer = AllerRetourSchema.dataContext.``public.customersEntity``
type Profile = AllerRetourSchema.dataContext.``public.customer_profilesEntity``
type EmailConfirmationToken =
  AllerRetourSchema.dataContext.``public.email_confirmation_tokensEntity``
type PasswordResetToken =
  AllerRetourSchema.dataContext.``public.password_reset_tokensEntity``

let ctx = AllerRetourSchema.GetDataContext ()
let submit = ctx.SubmitUpdates
let customers = ctx.Public.Customers
let customerProfiles = ctx.Public.CustomerProfiles
let emailConfirmationTokens = ctx.Public.EmailConfirmationTokens
let passwordResetTokens = ctx.Public.PasswordResetTokens
