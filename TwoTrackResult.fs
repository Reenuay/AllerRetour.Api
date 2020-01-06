namespace AllerRetour

type TwoTrackResult<'TSuccess, 'TFalure> =
  | Success of 'TSuccess
  | Failure of 'TFalure

type TwoTrackResultBuilder () =
  member _.Bind(x, f) =
    match x with
    | Success s -> f s
    | Failure f -> Failure f
  member _.Return(x) = Success x
  member _.ReturnFrom(x) = x

module TwoTrackResult =
  let result = TwoTrackResultBuilder()

  let succeed = Success

  let fail = Failure

  let either fSuccess fFailure = function
  | Success s -> fSuccess s
  | Failure f -> fFailure f

  let map f = either (f >> succeed) (fail)

  let bind f = either f fail

  let bindFailure f = either succeed f

  let eitherTeeR fSuccess fFailure
    = either (tee fSuccess >> succeed) (tee fFailure >> fail)

  let mult fSuccess fFailure switch1 switch2 x =
    match (switch1 x),(switch2 x) with
    | Success s1, Success s2 -> Success (fSuccess s1 s2)
    | Failure f1, Success _  -> Failure f1
    | Success _,  Failure f2 -> Failure f2
    | Failure f1, Failure f2 -> Failure (fFailure f1 f2)

  let (++) v1 v2 x =
    let ok _ _ = x
    let error e1 e2 = e1 @ e2
    mult ok error v1 v2 x

  let ifR rSuccess rFailure = function
  | true  -> Success rSuccess
  | false -> Failure rFailure

  let failIfFalse rFailure = ifR () rFailure

  let chain predicate rFailure x =
    predicate x
    |> ifR x rFailure

  let adapt switch map x =
    x
    |> map
    |> switch
    |> either (succeed x |> ignore2) fail

  let failIfNone rFailure = function
  | Some x -> Success x
  | None   -> Failure rFailure

  let failIfSome rFailure = function
  | Some _ -> Failure rFailure
  | None   -> Success ()
