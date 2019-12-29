module AllerRetour.Mail

open System.Net.Mail

[<AbstractClass; Sealed>]

type Settings private () =
  static member val Host = "" with get, set
  static member val Address = "" with get, set

type Mail = {
  To: string
  Subject: string
  Body: string
}

let send mail =
  use msg    = new MailMessage(Settings.Address, mail.To, mail.Subject, mail.Body)
  use client = new SmtpClient(Settings.Host)

  client.DeliveryMethod <- SmtpDeliveryMethod.Network

  client.Send msg
