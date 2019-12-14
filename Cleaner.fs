module Cleaner

open System.Text.RegularExpressions
let cleanWhiteSpace s = Regex.Replace(s, "\s", " ").Trim()
