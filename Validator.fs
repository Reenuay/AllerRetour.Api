module Validator

open System.Text.RegularExpressions

let isEmail s = Regex.IsMatch(s, "^\S+@\S+\.\S+$")

let hasMinLengthOf l s = s |> isNull |> not && String.length s >= l

let hasMaxLengthOf l s = String.length s <= l

let containsWords words s =
  Regex.IsMatch(
    s,
    "(" + (List.reduce (fun a b -> a + "|" + b) words) + ")",
    RegexOptions.IgnoreCase
  )
