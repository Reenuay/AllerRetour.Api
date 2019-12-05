module Dto

type Registration = {
  Email: string
  Password: string
}

module Registration =
  let fromJson s : Choice<Registration, string> = Json.deserialize s
  let toJson (r: Registration) = Json.serialize r
