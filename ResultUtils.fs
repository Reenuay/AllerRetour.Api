module ResultUtils

type ResultBuilder () =
  member _.Bind(x, f) =
    match x with
    | Ok    o -> f o
    | Error e -> Error e
  member _.Return(x) = Ok x
  member _.ReturnFrom(x) = x

let result = ResultBuilder()

let mult onOk onError switch1 switch2 x =
  match (switch1 x),(switch2 x) with
  | Ok o1   , Ok o2    -> Ok (onOk o1 o2)
  | Error e1, Ok _     -> Error e1
  | Ok _,     Error e2 -> Error e2
  | Error e1, Error e2 -> Error (onError e1 e2)

let tryCatch f x =
  try
    f x |> Ok
  with
  | x -> Error [x.Message]

let resultIf ok error = function
| true  -> Ok    ok
| false -> Error error

let errorIf error = resultIf () error

let chain predicate error x = resultIf x error (predicate x)

let adapt chainablePredicate mapper x =
  match mapper x |> chainablePredicate with
  | Ok _    -> Ok x
  | Error e -> Error e

let fromOption error = function
| Some x -> Ok x
| None   -> Error error

let (>>=) y x = Result.bind x y

let (++) v1 v2 =
  let ok o1 _ = o1
  let error e1 e2 = e1 @ e2
  mult ok error v1 v2

type AppError =
  | Validation of string list
  | NotFound   of string list
  | Conflict   of string list
  | Fatal      of string list

let toValidationError = function
| Error e -> Validation e |> Error
| Ok    o -> Ok o

let toNotFoundError = function
| Error e -> NotFound e |> Error
| Ok    o -> Ok o

let toConflictError = function
| Error e -> Conflict e |> Error
| Ok    o -> Ok o

let toFatalError = function
| Error e -> Fatal e |> Error
| Ok    o -> Ok o
