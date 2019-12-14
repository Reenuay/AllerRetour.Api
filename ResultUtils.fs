module ResultUtils

type ResultBuilder () =
  member _.Bind(x, f) =
    match x with
    | Ok    o -> f o
    | Error e -> Error e
  member _.Return(x) = Ok x
  member _.ReturnFrom(x) = x

let result = ResultBuilder()

let plus switch1 switch2 x =
  match (switch1 x),(switch2 x) with
  | Ok _,    Ok _     -> Ok x
  | Error e, Ok _     -> Error e
  | Ok _,    Error e  -> Error e
  | Error e1,Error e2 -> Error (e1 @ e2)

let tryCatch f x =
  try
    f x |> Ok
  with
  | x -> Error [x.Message]

let resultIf ok error = function
| true  -> Ok    ok
| false -> Error error

let checkIf predicate error x = resultIf x error (predicate x)

let adapt f x r =
  match x r |> f with
  | Ok _    -> Ok r
  | Error e -> Error e

let (>>=) y x = Result.bind x y

let (++) = plus

type AppError =
  | Validation of string list
  | Conflict   of string list
  | Fatal      of string list

let toValidationError = function
| Error e -> Validation e |> Error
| Ok    o -> Ok o

let toConflictError = function
| Error e -> Conflict e |> Error
| Ok    o -> Ok o

let toFatalError = function
| Error e -> Fatal e |> Error
| Ok    o -> Ok o
