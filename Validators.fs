module Validators

open System
open System.Text.RegularExpressions
open ResultBuilder

let email s =
  match Regex.IsMatch(s, "^\S+@\S+\.\S+$") with
  | true  -> Ok s
  | false -> Error ["Bad email format"]

let private passwordAllowedCharacters = "-+@#%&'.,*^$?()_!:;|~`".Split("")
let private passwordAllowedCharactersString = String.Join ("", passwordAllowedCharacters)
let private passwordMinLength = 8

let checkLength s =
  match s |> isNull |> not && String.length s >= passwordMinLength with
  | true  -> Ok s
  | false -> Error [(sprintf "Password must be at least %i characters length" passwordMinLength)]

let checkpasswordAllowedCharacters s =
  match Regex.IsMatch(s, sprintf "^[\d\p{L}%s]+$" passwordAllowedCharactersString) with
  | true
    -> Ok s
  | false
    -> Error [
      (", ", passwordAllowedCharacters)
      |> String.Join
      |> sprintf "Allowed characters are letters digits and of the %s"
    ]

let checkDigits s =
  match Regex.IsMatch(s, "\d") with
  | true  -> Ok s
  | false -> Error ["Password must containt at least one digit"]

let checkLetters s =
  match Regex.IsMatch(s, "(?=.*\p{Ll})(?=.*\p{Lu})") with
  | true  -> Ok s
  | false -> Error ["Password must contain at least one upper and one lower case letter"]

let checkSymbols s =
  match Regex.IsMatch(s, sprintf "[%s]" passwordAllowedCharactersString) with
  | true  -> Ok s
  | false -> Error ["Password must containt at least one special character"]

let password =
  checkLength
  ++ checkpasswordAllowedCharacters
  ++ checkDigits
  ++ checkLetters
  ++ checkSymbols

let private nameMaxLength = 100

let trimName s = Regex.Replace(s, "\s", " ").Trim()

let name s field =
  match s with
  | null | _ when String.length s = 0
    -> Error [sprintf "%s must contain at least 1 character" field]
  | _ when String.length s > nameMaxLength
    -> Error [sprintf "Length of %s should not exceed %i characters" field nameMaxLength]
  | _ -> Ok s
