using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.Domain.Interfaces.Services;

namespace PartnershipManager.Infrastructure.Services;

/// <summary>
/// Serviço de e-mail via Google Workspace SMTP Relay (smtp-relay.gmail.com:587).
/// Suporta autenticação por credenciais (username/password) ou por IP autorizado
/// (sem credenciais) — conforme configuração no Google Admin Console.
/// </summary>
public sealed class GoogleSmtpEmailService : IEmailService
{
    private readonly IEmailLogRepository _emailLogRepository;
    private readonly ILogger<GoogleSmtpEmailService> _logger;

    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly bool _enableSsl;
    private readonly string? _username;
    private readonly string? _password;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ResendEmailSender? _resend;

    public GoogleSmtpEmailService(
        IConfiguration configuration,
        IEmailLogRepository emailLogRepository,
        ILogger<GoogleSmtpEmailService> logger)
    {
        _emailLogRepository = emailLogRepository;
        _logger = logger;

        var section = configuration.GetSection("Email");
        _smtpHost   = section["SmtpHost"]   ?? "smtp-relay.gmail.com";
        _smtpPort   = int.Parse(section["SmtpPort"] ?? "587");
        _enableSsl  = bool.Parse(section["EnableSsl"]  ?? "true");
        _username   = section["Username"];
        // Suporta senha em texto puro ou codificada em Base64 (detecção automática)
        _password   = DecodePasswordIfBase64(section["Password"]);
        _fromEmail  = section["FromEmail"]  ?? "noreply@example.com";
        _fromName   = section["FromName"]   ?? "P-Pro | WP Manager";

        if (string.Equals(section["Provider"], "Resend", StringComparison.OrdinalIgnoreCase))
        {
            _resend = new ResendEmailSender(
                section["Resend:ApiKey"] ?? string.Empty,
                section["Resend:BaseUrl"] ?? "https://api.resend.com");
        }
    }

    /// <inheritdoc/>
    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string toName,
        string resetLink,
        string language)
    {
        var logId = Guid.NewGuid();
        var log = new EmailLog
        {
            Id          = logId,
            RecipientEmail = toEmail,
            RecipientName  = toName,
            Subject        = BuildSubject(language),
            TemplateName   = "password-reset",
            ReferenceType  = "password_reset",
            Status         = "queued",
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow
        };

        await _emailLogRepository.CreateAsync(log);

        try
        {
            var message = BuildMessage(toEmail, toName, resetLink, language);
            await SendAsync(message);

            await _emailLogRepository.UpdateStatusAsync(logId, "sent");
            _logger.LogInformation("E-mail de recuperação de senha enviado para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            await _emailLogRepository.UpdateStatusAsync(logId, "failed", ex.Message);
            _logger.LogError(ex, "Falha ao enviar e-mail de recuperação de senha para {Email}", toEmail);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // Helpers privados
    // -----------------------------------------------------------------------

    // Detecta e decodifica senha em Base64; retorna o valor original se não for Base64 válido
    private static string? DecodePasswordIfBase64(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value));
            return decoded.All(c => c >= 32 && c < 127) ? decoded : value;
        }
        catch
        {
            return value;
        }
    }

    private async Task SendAsync(MimeMessage message)
    {
        if (_resend is not null)
        {
            await _resend.SendAsync(message);
            return;
        }

        using var client = new SmtpClient();

        var socketOptions = _enableSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await client.ConnectAsync(_smtpHost, _smtpPort, socketOptions);

        if (!string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password))
        {
            await client.AuthenticateAsync(_username, _password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private MimeMessage BuildMessage(
        string toEmail, string toName, string resetLink, string language)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = BuildSubject(language);

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = BuildHtmlBody(toName, resetLink, language),
            TextBody = BuildTextBody(toName, resetLink, language)
        };
        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }

    private static string BuildSubject(string language) =>
        language.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? "P-Pro | WP Manager — Reset your password"
            : "P-Pro | WP Manager — Recuperação de senha";

    private static string BuildHtmlBody(string toName, string resetLink, string language)
    {
        bool ptBr = !language.StartsWith("en", StringComparison.OrdinalIgnoreCase);

        string greeting    = ptBr ? $"Olá, {toName}!" : $"Hello, {toName}!";
        string line1       = ptBr
            ? "Recebemos uma solicitação para redefinição da senha da sua conta."
            : "We received a request to reset your account password.";
        string line2       = ptBr
            ? "Clique no botão abaixo para criar uma nova senha. O link é válido por <strong>1 hora</strong>."
            : "Click the button below to create a new password. The link is valid for <strong>1 hour</strong>.";
        string btnText     = ptBr ? "Redefinir Senha" : "Reset Password";
        string line3       = ptBr
            ? "Se você não solicitou isso, ignore este e-mail — sua senha permanece a mesma."
            : "If you did not request this, please ignore this email — your password remains unchanged.";
        string footer      = ptBr
            ? "Este e-mail foi enviado automaticamente. Por favor, não responda."
            : "This is an automated message. Please do not reply.";

        return $@"<!DOCTYPE html>
<html lang=""{(ptBr ? "pt-BR" : "en")}"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>{BuildSubject(language)}</title>
</head>
<body style=""margin:0;padding:0;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f3f4f6;padding:40px 0;"">
    <tr>
      <td align=""center"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);"">
          <!-- Header -->
          <tr>
            <td style=""background-color:#111827;padding:32px 40px;text-align:center;"">
              <span style=""color:#ffffff;font-size:22px;font-weight:bold;letter-spacing:1px;"">P-Pro | WP Manager</span>
            </td>
          </tr>
          <!-- Body -->
          <tr>
            <td style=""padding:40px;"">
              <p style=""color:#111827;font-size:18px;font-weight:bold;margin:0 0 16px;"">{greeting}</p>
              <p style=""color:#374151;font-size:15px;line-height:1.6;margin:0 0 12px;"">{line1}</p>
              <p style=""color:#374151;font-size:15px;line-height:1.6;margin:0 0 32px;"">{line2}</p>
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                  <td align=""center"" style=""padding:0 0 32px;"">
                    <a href=""{resetLink}""
                       style=""display:inline-block;background-color:#0891B2;color:#ffffff;font-size:16px;font-weight:bold;text-decoration:none;padding:14px 36px;border-radius:6px;"">
                      {btnText}
                    </a>
                  </td>
                </tr>
              </table>
              <p style=""color:#6b7280;font-size:13px;line-height:1.6;margin:0 0 12px;"">{line3}</p>
              <p style=""color:#9ca3af;font-size:12px;margin:0;"">{footer}</p>
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td style=""background-color:#f9fafb;padding:20px 40px;text-align:center;"">
              <p style=""color:#9ca3af;font-size:12px;margin:0;"">© {DateTime.UtcNow.Year} P-Pro | WP Manager</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    private static string BuildTextBody(string toName, string resetLink, string language)
    {
        bool ptBr = !language.StartsWith("en", StringComparison.OrdinalIgnoreCase);

        return ptBr
            ? $"Olá, {toName}!\n\nRecebemos uma solicitação para redefinição da sua senha.\n\nAcesse o link abaixo para criar uma nova senha (válido por 1 hora):\n{resetLink}\n\nSe você não solicitou isso, ignore este e-mail.\n\nP-Pro | WP Manager"
            : $"Hello, {toName}!\n\nWe received a request to reset your password.\n\nClick the link below to create a new password (valid for 1 hour):\n{resetLink}\n\nIf you did not request this, please ignore this email.\n\nP-Pro | WP Manager";
    }

    // -----------------------------------------------------------------------
    // Account Activation e-mail
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task SendAccountActivationEmailAsync(
        string toEmail,
        string toName,
        string activationLink,
        string language)
    {
        var logId = Guid.NewGuid();
        var log = new EmailLog
        {
            Id             = logId,
            RecipientEmail = toEmail,
            RecipientName  = toName,
            Subject        = BuildActivationSubject(language),
            TemplateName   = "account-activation",
            ReferenceType  = "account_activation",
            Status         = "queued",
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow
        };

        await _emailLogRepository.CreateAsync(log);

        try
        {
            var message = BuildActivationMessage(toEmail, toName, activationLink, language);
            await SendAsync(message);

            await _emailLogRepository.UpdateStatusAsync(logId, "sent");
            _logger.LogInformation("E-mail de ativação de conta enviado para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            await _emailLogRepository.UpdateStatusAsync(logId, "failed", ex.Message);
            _logger.LogError(ex, "Falha ao enviar e-mail de ativação de conta para {Email}", toEmail);
            throw;
        }
    }

    private MimeMessage BuildActivationMessage(
        string toEmail, string toName, string activationLink, string language)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = BuildActivationSubject(language);

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = BuildActivationHtmlBody(toName, activationLink, language),
            TextBody = BuildActivationTextBody(toName, activationLink, language)
        };
        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }

    private static string BuildActivationSubject(string language) =>
        language.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? "P-Pro | WP Manager — Activate your account"
            : "P-Pro | WP Manager — Ative sua conta";

    private static string BuildActivationHtmlBody(string toName, string activationLink, string language)
    {
        bool ptBr = !language.StartsWith("en", StringComparison.OrdinalIgnoreCase);

        string greeting = ptBr ? $"Bem-vindo, {toName}!" : $"Welcome, {toName}!";
        string line1    = ptBr
            ? "Sua conta foi criada no P-Pro | WP Manager."
            : "Your account has been created in P-Pro | WP Manager.";
        string line2    = ptBr
            ? "Clique no botão abaixo para definir sua senha e ativar o acesso. O link é válido por <strong>72 horas</strong>."
            : "Click the button below to set your password and activate your access. The link is valid for <strong>72 hours</strong>.";
        string btnText  = ptBr ? "Ativar Minha Conta" : "Activate My Account";
        string line3    = ptBr
            ? "Se você não esperava este convite, pode ignorar este e-mail com segurança."
            : "If you weren't expecting this invitation, you can safely ignore this email.";
        string footer   = ptBr
            ? "Este e-mail foi enviado automaticamente. Por favor, não responda."
            : "This is an automated message. Please do not reply.";

        return $@"<!DOCTYPE html>
<html lang=""{(ptBr ? "pt-BR" : "en")}"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>{BuildActivationSubject(language)}</title>
</head>
<body style=""margin:0;padding:0;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f3f4f6;padding:40px 0;"">
    <tr>
      <td align=""center"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);"">
          <!-- Header -->
          <tr>
            <td style=""background-color:#111827;padding:32px 40px;text-align:center;"">
              <span style=""color:#ffffff;font-size:22px;font-weight:bold;letter-spacing:1px;"">P-Pro | WP Manager</span>
            </td>
          </tr>
          <!-- Body -->
          <tr>
            <td style=""padding:40px;"">
              <p style=""color:#111827;font-size:18px;font-weight:bold;margin:0 0 16px;"">{greeting}</p>
              <p style=""color:#374151;font-size:15px;line-height:1.6;margin:0 0 12px;"">{line1}</p>
              <p style=""color:#374151;font-size:15px;line-height:1.6;margin:0 0 32px;"">{line2}</p>
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                  <td align=""center"" style=""padding:0 0 32px;"">
                    <a href=""{activationLink}""
                       style=""display:inline-block;background-color:#059669;color:#ffffff;font-size:16px;font-weight:bold;text-decoration:none;padding:14px 36px;border-radius:6px;"">
                      {btnText}
                    </a>
                  </td>
                </tr>
              </table>
              <p style=""color:#6b7280;font-size:13px;line-height:1.6;margin:0 0 12px;"">{line3}</p>
              <p style=""color:#9ca3af;font-size:12px;margin:0;"">{footer}</p>
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td style=""background-color:#f9fafb;padding:20px 40px;text-align:center;"">
              <p style=""color:#9ca3af;font-size:12px;margin:0;"">© {DateTime.UtcNow.Year} P-Pro | WP Manager</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    private static string BuildActivationTextBody(string toName, string activationLink, string language)
    {
        bool ptBr = !language.StartsWith("en", StringComparison.OrdinalIgnoreCase);

        return ptBr
            ? $"Bem-vindo, {toName}!\n\nSua conta foi criada no P-Pro | WP Manager.\n\nAcesse o link abaixo para definir sua senha e ativar o acesso (válido por 72 horas):\n{activationLink}\n\nSe você não esperava este convite, pode ignorar este e-mail.\n\nP-Pro | WP Manager"
            : $"Welcome, {toName}!\n\nYour account has been created in P-Pro | WP Manager.\n\nClick the link below to set your password and activate your access (valid for 72 hours):\n{activationLink}\n\nIf you weren't expecting this invitation, you can safely ignore this email.\n\nP-Pro | WP Manager";
    }

    // -----------------------------------------------------------------------
    // Communication Published e-mail
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task SendCommunicationPublishedEmailAsync(
        string toEmail, string toName,
        string commTitle, string? commSummary, string commType,
        string actionUrl, string companyName)
    {
        var logId = Guid.NewGuid();
        var subject = $"P-Pro | {companyName} — Nova comunicação: {commTitle}";
        var log = new EmailLog
        {
            Id             = logId,
            RecipientEmail = toEmail,
            RecipientName  = toName,
            Subject        = subject,
            TemplateName   = "communication-published",
            ReferenceType  = "communication",
            Status         = "queued",
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow
        };
        await _emailLogRepository.CreateAsync(log);
        try
        {
            var commTypeLabel = commType switch
            {
                "announcement" => "Anúncio",
                "update"       => "Atualização",
                "report"       => "Relatório",
                "alert"        => "Alerta",
                "invitation"   => "Convite",
                _              => commType
            };
            var bodyText = !string.IsNullOrWhiteSpace(commSummary)
                ? commSummary
                : $"Nova comunicação publicada por {companyName}.";

            var html = $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head><meta charset=""UTF-8""/><meta name=""viewport"" content=""width=device-width,initial-scale=1.0""/></head>
<body style=""margin:0;padding:0;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f3f4f6;padding:40px 0;"">
  <tr><td align=""center"">
    <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);"">
      <tr><td style=""background-color:#111827;padding:32px 40px;text-align:center;"">
        <span style=""color:#ffffff;font-size:22px;font-weight:bold;letter-spacing:1px;"">P-Pro | WP Manager</span>
      </td></tr>
      <tr><td style=""padding:40px;"">
        <p style=""color:#6b7280;font-size:12px;font-weight:600;letter-spacing:1px;text-transform:uppercase;margin:0 0 8px;"">{commTypeLabel}</p>
        <p style=""color:#111827;font-size:20px;font-weight:bold;margin:0 0 16px;"">{commTitle}</p>
        <p style=""color:#374151;font-size:15px;line-height:1.6;margin:0 0 8px;"">Olá, {toName}!</p>
        <p style=""color:#374151;font-size:15px;line-height:1.6;margin:0 0 32px;"">{bodyText}</p>
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
          <tr><td align=""center"" style=""padding:0 0 32px;"">
            <a href=""{actionUrl}"" style=""display:inline-block;background-color:#0891B2;color:#ffffff;font-size:16px;font-weight:bold;text-decoration:none;padding:14px 36px;border-radius:6px;"">
              Ver Comunicação
            </a>
          </td></tr>
        </table>
        <p style=""color:#9ca3af;font-size:12px;margin:0;"">Este e-mail foi enviado automaticamente por {companyName} via P-Pro. Por favor, não responda.</p>
      </td></tr>
      <tr><td style=""background-color:#f9fafb;padding:20px 40px;text-align:center;"">
        <p style=""color:#9ca3af;font-size:12px;margin:0;"">© {DateTime.UtcNow.Year} P-Pro | WP Manager</p>
      </td></tr>
    </table>
  </td></tr>
</table>
</body></html>";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            var bb = new BodyBuilder
            {
                HtmlBody = html,
                TextBody = $"Olá, {toName}!\n\n{bodyText}\n\nAcesse: {actionUrl}\n\nP-Pro | WP Manager"
            };
            message.Body = bb.ToMessageBody();
            await SendAsync(message);
            await _emailLogRepository.UpdateStatusAsync(logId, "sent");
            _logger.LogInformation("E-mail de comunicação publicada enviado para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            await _emailLogRepository.UpdateStatusAsync(logId, "failed", ex.Message);
            _logger.LogError(ex, "Falha ao enviar e-mail de comunicação para {Email}", toEmail);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // Approval Assigned e-mail
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task SendApprovalAssignedEmailAsync(
        string toEmail, string toName,
        string workflowTitle, string stepName, string requesterName,
        string priority, DateTime? dueDate, string actionUrl)
    {
        var logId = Guid.NewGuid();
        var subject = $"P-Pro | Aprovação solicitada: {workflowTitle}";
        var log = new EmailLog
        {
            Id             = logId,
            RecipientEmail = toEmail,
            RecipientName  = toName,
            Subject        = subject,
            TemplateName   = "approval-assigned",
            ReferenceType  = "workflow",
            Status         = "queued",
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow
        };
        await _emailLogRepository.CreateAsync(log);
        try
        {
            var priorityLabel = priority switch
            {
                "low"    => "Baixa",
                "medium" => "Média",
                "high"   => "Alta",
                "urgent" => "Urgente",
                _        => priority
            };
            var priorityColor = priority switch
            {
                "urgent" => "#DC2626",
                "high"   => "#D97706",
                "medium" => "#2563EB",
                _        => "#6B7280"
            };
            var dueDateStr = dueDate.HasValue
                ? $"<p style=\"color:#374151;font-size:14px;margin:0 0 8px;\"><strong>Prazo:</strong> {dueDate.Value:dd/MM/yyyy}</p>"
                : "";

            var html = $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head><meta charset=""UTF-8""/><meta name=""viewport"" content=""width=device-width,initial-scale=1.0""/></head>
<body style=""margin:0;padding:0;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f3f4f6;padding:40px 0;"">
  <tr><td align=""center"">
    <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);"">
      <tr><td style=""background-color:#111827;padding:32px 40px;text-align:center;"">
        <span style=""color:#ffffff;font-size:22px;font-weight:bold;letter-spacing:1px;"">P-Pro | WP Manager</span>
      </td></tr>
      <tr><td style=""padding:40px;"">
        <p style=""color:#374151;font-size:15px;line-height:1.6;margin:0 0 8px;"">Olá, {toName}!</p>
        <p style=""color:#374151;font-size:15px;line-height:1.6;margin:0 0 24px;"">Você foi designado como aprovador em um fluxo de aprovação.</p>
        <div style=""background-color:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;padding:24px;margin:0 0 32px;"">
          <p style=""color:#111827;font-size:17px;font-weight:bold;margin:0 0 12px;"">{workflowTitle}</p>
          <p style=""color:#374151;font-size:14px;margin:0 0 8px;""><strong>Etapa:</strong> {stepName}</p>
          <p style=""color:#374151;font-size:14px;margin:0 0 8px;""><strong>Solicitado por:</strong> {requesterName}</p>
          <p style=""color:#374151;font-size:14px;margin:0 0 8px;""><strong>Prioridade:</strong> <span style=""color:{priorityColor};font-weight:600;"">{priorityLabel}</span></p>
          {dueDateStr}
        </div>
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
          <tr><td align=""center"" style=""padding:0 0 32px;"">
            <a href=""{actionUrl}"" style=""display:inline-block;background-color:#0891B2;color:#ffffff;font-size:16px;font-weight:bold;text-decoration:none;padding:14px 36px;border-radius:6px;"">
              Revisar e Aprovar
            </a>
          </td></tr>
        </table>
        <p style=""color:#9ca3af;font-size:12px;margin:0;"">Este e-mail foi enviado automaticamente. Por favor, não responda.</p>
      </td></tr>
      <tr><td style=""background-color:#f9fafb;padding:20px 40px;text-align:center;"">
        <p style=""color:#9ca3af;font-size:12px;margin:0;"">© {DateTime.UtcNow.Year} P-Pro | WP Manager</p>
      </td></tr>
    </table>
  </td></tr>
</table>
</body></html>";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            var bb = new BodyBuilder
            {
                HtmlBody = html,
                TextBody = $"Olá, {toName}!\n\nVocê foi designado como aprovador em: {workflowTitle}\nEtapa: {stepName}\nSolicitado por: {requesterName}\nPrioridade: {priorityLabel}\n\nAcesse: {actionUrl}\n\nP-Pro | WP Manager"
            };
            message.Body = bb.ToMessageBody();
            await SendAsync(message);
            await _emailLogRepository.UpdateStatusAsync(logId, "sent");
            _logger.LogInformation("E-mail de aprovação atribuída enviado para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            await _emailLogRepository.UpdateStatusAsync(logId, "failed", ex.Message);
            _logger.LogError(ex, "Falha ao enviar e-mail de aprovação para {Email}", toEmail);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // Approval Decision e-mail
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task SendApprovalDecisionEmailAsync(
        string toEmail, string toName,
        string workflowTitle, string finalStatus, string approverName,
        string? comments, string actionUrl)
    {
        var logId = Guid.NewGuid();
        bool approved = string.Equals(finalStatus, "approved", StringComparison.OrdinalIgnoreCase);
        var statusLabel = approved ? "Aprovado" : "Rejeitado";
        var subject = $"P-Pro | Fluxo {statusLabel}: {workflowTitle}";
        var log = new EmailLog
        {
            Id             = logId,
            RecipientEmail = toEmail,
            RecipientName  = toName,
            Subject        = subject,
            TemplateName   = "approval-decision",
            ReferenceType  = "workflow",
            Status         = "queued",
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow
        };
        await _emailLogRepository.CreateAsync(log);
        try
        {
            var headerColor = approved ? "#059669" : "#DC2626";
            var headerText  = approved ? "✓ Aprovado" : "✗ Rejeitado";
            var commentsHtml = !string.IsNullOrWhiteSpace(comments)
                ? $"<div style=\"background-color:#f9fafb;border-left:4px solid {headerColor};padding:12px 16px;margin:16px 0;\"><p style=\"color:#374151;font-size:14px;margin:0;\"><strong>Comentários:</strong> {comments}</p></div>"
                : "";

            var html = $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head><meta charset=""UTF-8""/><meta name=""viewport"" content=""width=device-width,initial-scale=1.0""/></head>
<body style=""margin:0;padding:0;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f3f4f6;padding:40px 0;"">
  <tr><td align=""center"">
    <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);"">
      <tr><td style=""background-color:#111827;padding:32px 40px;text-align:center;"">
        <span style=""color:#ffffff;font-size:22px;font-weight:bold;letter-spacing:1px;"">P-Pro | WP Manager</span>
      </td></tr>
      <tr><td style=""padding:40px;"">
        <div style=""text-align:center;margin:0 0 24px;"">
          <span style=""display:inline-block;background-color:{headerColor};color:#ffffff;font-size:18px;font-weight:bold;padding:10px 28px;border-radius:6px;"">{headerText}</span>
        </div>
        <p style=""color:#374151;font-size:15px;line-height:1.6;margin:0 0 8px;"">Olá, {toName}!</p>
        <p style=""color:#374151;font-size:15px;line-height:1.6;margin:0 0 24px;"">O fluxo de aprovação <strong>{workflowTitle}</strong> foi <strong>{statusLabel.ToLower()}</strong> por <strong>{approverName}</strong>.</p>
        {commentsHtml}
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
          <tr><td align=""center"" style=""padding:24px 0 32px;"">
            <a href=""{actionUrl}"" style=""display:inline-block;background-color:#0891B2;color:#ffffff;font-size:16px;font-weight:bold;text-decoration:none;padding:14px 36px;border-radius:6px;"">
              Ver Fluxo
            </a>
          </td></tr>
        </table>
        <p style=""color:#9ca3af;font-size:12px;margin:0;"">Este e-mail foi enviado automaticamente. Por favor, não responda.</p>
      </td></tr>
      <tr><td style=""background-color:#f9fafb;padding:20px 40px;text-align:center;"">
        <p style=""color:#9ca3af;font-size:12px;margin:0;"">© {DateTime.UtcNow.Year} P-Pro | WP Manager</p>
      </td></tr>
    </table>
  </td></tr>
</table>
</body></html>";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            var bb = new BodyBuilder
            {
                HtmlBody = html,
                TextBody = $"Olá, {toName}!\n\nO fluxo \"{workflowTitle}\" foi {statusLabel.ToLower()} por {approverName}.\n{(string.IsNullOrWhiteSpace(comments) ? "" : $"Comentários: {comments}\n")}\nAcesse: {actionUrl}\n\nP-Pro | WP Manager"
            };
            message.Body = bb.ToMessageBody();
            await SendAsync(message);
            await _emailLogRepository.UpdateStatusAsync(logId, "sent");
            _logger.LogInformation("E-mail de decisão de aprovação enviado para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            await _emailLogRepository.UpdateStatusAsync(logId, "failed", ex.Message);
            _logger.LogError(ex, "Falha ao enviar e-mail de decisão para {Email}", toEmail);
            throw;
        }
    }
}
