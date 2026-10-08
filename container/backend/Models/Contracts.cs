using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace Backend.Models;

public sealed class AppError(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
public static class Input
{
    public static string Email(string? value)
    {
        var email = (value ?? "").Trim().ToLowerInvariant();
        if (email.Length > 254 || email.Contains('\r') || email.Contains('\n') ||
            !MailAddress.TryCreate(email, out var address) || address.Address != email ||
            !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^\s@<>]+@[^\s@<>]+\.[^\s@<>]+$"))
            throw new AppError(400, "Zadej jednu platnou e-mailovou adresu.");
        return email;
    }
    public static string CorporateEmail(string? value)
    {
        var email = Email(value);
        var domain = email.Split('@')[1];
        if(domain is not ("alza.cz" or "alza.sk") ||
            !System.Text.RegularExpressions.Regex.IsMatch(email,@"^[a-z0-9._+-]+@(?:alza\.cz|alza\.sk)$"))
            throw new AppError(400,"Použij konkrétní firemní adresu @alza.cz nebo @alza.sk.");
        return email;
    }
    public static string Text(string? value, int min, int max, string label)
    {
        var text = (value ?? "").Trim();
        if (text.Length < min || text.Length > max) throw new AppError(400, $"{label}: povolená délka je {min}–{max} znaků.");
        return text;
    }
    public static string Choice(string? value, params string[] allowed)
    {
        if (value is null || !allowed.Contains(value)) throw new AppError(400, "Nepovolená hodnota nastavení.");
        return value;
    }
}
public sealed record DraftRequest(string Recipient, string Instructions, string Tone, string Language)
{
    public DraftRequest Validated() => new(Input.Email(Recipient), Input.Text(Instructions,10,4000,"Zadání"),
        Input.Choice(Tone,"professional","friendly","concise"), Input.Choice(Language,"cs","en"));
}
public sealed record DraftResult(string Subject, string Body, bool Simulated);
public sealed record SendRequest(Guid RequestId, string Recipient, string Subject, string Body)
{
    public SendRequest Validated()
    {
        if (RequestId == Guid.Empty) throw new AppError(400,"Chybí identifikátor odeslání.");
        var subject = Input.Text(Subject,1,200,"Předmět");
        if(subject.Contains('\r') || subject.Contains('\n')) throw new AppError(400,"Předmět musí být na jednom řádku.");
        return new(RequestId,Input.Email(Recipient),subject,Input.Text(Body,1,10000,"Text e-mailu"));
    }
    public string Fingerprint() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new {Recipient,Subject,Body}))));
}
public sealed record AccessDecision(bool Allowed, bool IsAdmin);
public sealed record AccessState(string[] Admins, string[] Users, string Mode);
public sealed record MemberRequest(string Email, string Role);
public sealed record ModeRequest(string Mode);
public sealed record SendReservation(bool IsNew, string Status);
public sealed record TestPreview(string[] Columns, string[][] Rows, bool Simulated, string Source);

public sealed record AccessRequest(string Email,string Status,DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt=null,string? ReviewedBy=null);
public sealed record ReviewRequest(string Email,string Decision);
