module AllerRetour.Normalizers

open System.Text.RegularExpressions

let cleanWhiteSpace s = Regex.Replace(s, "\s", " ").Trim()
