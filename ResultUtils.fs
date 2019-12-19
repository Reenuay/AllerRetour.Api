module ResultUtils

type ResultBuilder () =
  member _.Bind(x, f) =
    match x with
    | Ok    o -> f o
    | Error e -> Error e
  member _.Return(x) = Ok x
  member _.ReturnFrom(x) = x

let result = ResultBuilder()

let switch f x = f x |> Ok

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

let falseTo error = resultIf () error

let chain predicate error x = resultIf x error (predicate x)

let adapt chainablePredicate mapper x =
  match mapper x |> chainablePredicate with
  | Ok _    -> Ok x
  | Error e -> Error e

let fromOption error = function
| Some x -> Ok x
| None   -> Error error

let (>>=) y x = Result.bind x y

let (++) v1 v2 x =
  let ok _ _ = x
  let error e1 e2 = e1 @ e2
  mult ok error v1 v2 x

type AppError =
  | ValidationError of string list
  | NotFoundError   of string list
  | ConflictError   of string list
  | FatalError      of string list

let toAppError (error: string list -> AppError) x =
  match x with
  | Ok o -> Ok o
  | Error e -> error e |> Error

let toValidationError x = toAppError ValidationError x
let toNotFoundError x = toAppError NotFoundError x
let toConflictError x = toAppError ConflictError x
let toFatalError x = toAppError FatalError x
