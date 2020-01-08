namespace AllerRetour.Globals

[<AbstractClass; Sealed>]
type Server private () =
  static member val Host = "" with get, set

[<AbstractClass; Sealed>]
type Mail private () =
  static member val Host = "" with get, set
  static member val Address = "" with get, set

[<AbstractClass; Sealed>]
type Auth private () =
  static member val Secret = "" with get, set
  static member val Issuer = "" with get, set
  static member val Audience = "" with get, set
