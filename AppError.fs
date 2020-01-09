namespace AllerRetour

type AppError =
  // Generic errors
  | Validation of string list
  | DbError of string

  // Domain Errors
  | EmailIsAlreadyRegistered of string
  | EmailIsNotConfirmed of string
  | CustomerNotFound of string
  | InvalidPassword of string
  | TokenNotFound of string
