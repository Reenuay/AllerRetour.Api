namespace AllerRetour

open TwoTrackResult

type AppErrorCase =
  | Validation
  | Unauthorized
  | NotFound
  | Conflict
  | Fatal

type AppError = AppError of AppErrorCase * string list

module AppError =
  let create case x = AppError (case, x)

  let specify case = either succeed (create case >> fail)

  let getMessages (AppError (_, l)) = l
