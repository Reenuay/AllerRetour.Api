module Validator

open System.Text.RegularExpressions

let checkIsEmail field s =
  match Regex.IsMatch(s, "^\S+@\S+\.\S+$") with
  | true  -> Ok s
  | false -> Error [sprintf "%s has bad format" field]

let checkMinLength field l s =
  match s |> isNull |> not && String.length s >= l with
  | true  -> Ok s
  | false -> Error [sprintf "%s must be at least %i characters length" field l]

let checkMaxLength field l s =
  match String.length s <= l with
  | true  -> Ok s
  | false -> Error [sprintf "%s must not be longer that %i characters" field l]

let checkHasDigits field s =
  match Regex.IsMatch(s, "\d") with
  | true  -> Ok s
  | false -> Error [sprintf "%s must containt at least one digit" field]

let checkHasLetters field s =
  match Regex.IsMatch(s, "(?=.*\p{Ll})(?=.*\p{Lu})") with
  | true  -> Ok s
  | false -> Error [sprintf "%s must contain at least one upper and one lower case letter" field]

let checkHasSymbols field s =
  match Regex.IsMatch(s, "[\p{S}\p{P}]") with
  | true  -> Ok s
  | false -> Error [sprintf "%s must containt at least one special character" field]

let cleanWhiteSpace s = Regex.Replace(s, "\s", " ").Trim()
