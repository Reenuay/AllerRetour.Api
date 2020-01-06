namespace AllerRetour

type AppError =
  // Generic errors
  | Validation of string list
  | DbError of string

  // Specific errors
  | EmailIsAlreadyRegistered of string
  | CustomerNotFound of string
  | InvalidPassword of string
