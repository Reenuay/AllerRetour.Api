module ResultBuilder

type AppError =
  | Validation of string list
  | Conflict   of string list
  | Fatal      of string list

type ResultBuilder () =
  member _.Bind(x, f) =
    match x with
    | Ok    r -> f r
    | Error r -> Error r
  member _.Return(x) = Ok x
  member _.ReturnFrom(x) = x

let result = ResultBuilder()

let (>>=) y x = Result.bind x y

let plus switch1 switch2 x =
    match (switch1 x),(switch2 x) with
    | Ok _,    Ok _     -> Ok x
    | Error e, Ok _     -> Error e
    | Ok _,    Error e  -> Error e
    | Error e1,Error e2 -> Error (e1 @ e2)

let (++) = plus

let map2 success error = function
| Ok    s -> success s
| Error e -> error e

let tryCatch f x =
  try
    f x |> Ok
  with
  | x -> Error [x.Message]

let toValidationError = function
| Error e -> Validation e |> Error
| Ok    s -> Ok s

let toConflictError = function
| Error e -> Conflict e |> Error
| Ok    s -> Ok s

let toFatalError = function
| Error e -> Fatal e |> Error
| Ok    s -> Ok s

let errorIfTrue x y = if y then Error x else Ok ()
