using System.Net.Http.Headers;
using System.Net.Http.Json;
using MimeKit;

namespace PartnershipManager.Infrastructure.Services;

/// <summary>Envio de MimeMessage via API HTTP do Resend (POST /emails).</summary>
internal sealed class ResendEmailSender
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly string _apiKey;
    private readonly string _endpoint;

    public ResendEmailSender(string apiKey, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Email:Resend:ApiKey não configurada.");

        _apiKey = apiKey;
        _endpoint = baseUrl.TrimEnd('/') + "/emails";
    }

    public async Task SendAsync(MimeMessage message)
    {
        var from = message.From.Mailboxes.First();
        var payload = new
        {
            from = string.IsNullOrWhiteSpace(from.Name) ? from.Address : $"{from.Name} <{from.Address}>",
            to = message.To.Mailboxes.Select(m => m.Address).ToArray(),
            subject = message.Subject,
            html = message.HtmlBody,
            text = message.TextBody
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Resend retornou {(int)response.StatusCode}: {body}");
        }
    }
}
