module AllerRetour.Mail

open System.Net.Mail

type Mail = {
  To: string
  Subject: string
  Body: string
}

let send mail =
  use msg = new MailMessage(Globals.Mail.Address, mail.To, mail.Subject, mail.Body)
  msg.IsBodyHtml <- true

  use client = new SmtpClient(Globals.Mail.Host)
  client.DeliveryMethod <- SmtpDeliveryMethod.Network

  client.Send msg

let sendConfirm address code =
  let subject = "Please, confirm your email address"
  send {
    To = address
    Subject = subject
    Body = sprintf """
      This message was send to you from Aller Retour service because your email address
      was used for registration. Use the link below to confirm your email.
      If you didn't do that just ignore this message.
      <a href="%s/api/customer/confirm?email=%s&code=%s" target="_blank">Confirm Email</a>
    """ Globals.Server.Host address code
  }
