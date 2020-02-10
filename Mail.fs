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

let sendConfirm (email, token) =
  send {
    To = email
    Subject = "Please, confirm your email address"
    Body = sprintf """
      This message was send to you from Aller Retour service because your email address
      was used for registration. Use the link below to confirm your email.
      If you didn't do that just ignore this message.
      This link will expire in 12 hours.
      <a href="%s/api/customer/email/confirm?email=%s&token=%s" target="_blank">Confirm Email</a>
    """ Globals.Server.Host email token
  }

let sendReset (email, token) =
  send {
    To = email
    Subject = "Password reset"
    Body = sprintf """
      This message was send to you from Aller Retour service. If it weren't you just ignore it.
      Use the verification code below to reset your password.
      This code will expire in 15 minutes.
      <b>%s</b>
    """ token
  }
