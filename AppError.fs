namespace AllerRetour

open TwoTrackResult

type AppErrorCase =
  | ValidationError
  | UnauthorizedError
  | NotFoundError
  | ConflictError
  | FatalError

type AppError = AppError of AppErrorCase * string list

module AppError =
  let create case x = AppError (case, x)

  let specify case = either succeed (create case >> fail)

  let getMessages (AppError (_, l)) = l
