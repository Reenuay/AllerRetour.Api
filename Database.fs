module Database

open FSharp.Data.Sql

let [<Literal>] DbVendor = Common.DatabaseProviderTypes.POSTGRESQL
let [<Literal>] ConnString = "Host=127.0.0.1;Port=5432;Database=aller_retour;Username=aller_retour"
let [<Literal>] UseOptionTypes = true
let [<Literal>] ResolutionPath = @"../.nuget/packages/npgsql/4.1.2/lib/netcoreapp3.0"

type Sql =
  SqlDataProvider<
    DbVendor,
    ConnString,
    UseOptionTypes = UseOptionTypes,
    ResolutionPath = ResolutionPath
  >

let context = Sql.GetDataContext()
