using System.Text.Json;
using Backend.Access;
using Backend.Models;
namespace Backend.Services;
public sealed record MailSubmission(Guid RequestId,string Recipient,string Subject,string Body);
public sealed record DeliveryOutcome(string Status,bool Simulated);
public interface IMailSender {Task<DeliveryOutcome> SendAsync(MailSubmission mail,string actor,CancellationToken ct);}
public sealed class DemoMailSender : IMailSender
{
    public Task<DeliveryOutcome> SendAsync(MailSubmission mail,string actor,CancellationToken ct) => Task.FromResult(new DeliveryOutcome("simulated",true));
}
public sealed class N8nMailSender(HttpClient client,AppSettings settings) : IMailSender
{
    public async Task<DeliveryOutcome> SendAsync(MailSubmission mail,string actor,CancellationToken ct)
    {
        using var request=new HttpRequestMessage(HttpMethod.Post,settings.WebhookUrl);
        request.Headers.Add(settings.HeaderName,settings.AuthToken);
        request.Content=JsonContent.Create(new {requestId=mail.RequestId,to=mail.Recipient,subject=mail.Subject,body=mail.Body,requestedBy=actor});
        using var response=await client.SendAsync(request,ct);
        if(!response.IsSuccessStatusCode) throw new AppError(502,"n8n nepotvrdilo odeslání. Výsledek je nejistý; před dalším pokusem ověř běh workflow.");
        try
        {
            using var reply=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if(reply.RootElement.GetProperty("status").GetString()!="sent" ||
                !Guid.TryParse(reply.RootElement.GetProperty("requestId").GetString(),out var id) || id!=mail.RequestId)
                throw new JsonException();
        }
        catch(Exception exception) when(exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {throw new AppError(502,"n8n nevrátilo správné potvrzení. Před dalším pokusem ověř běh workflow.");}
        return new DeliveryOutcome("acknowledged",false);
    }
}
public sealed class DeliveryService(IAppStore store,IMailSender sender,AppSettings settings,ILogger<DeliveryService> logger)
{
    public async Task<object> SendAsync(SendRequest input,string actor,CancellationToken ct)
    {
        if(!settings.Local && !settings.MailConfigured)
            throw new AppError(503,"Odesílání e-mailů zatím není nastavené.");
        var reservation=await store.ReserveAsync(input.RequestId,actor,input.Fingerprint(),ct);
        if(!reservation.IsNew)
        {
            if(reservation.Status is "pending" or "uncertain") throw new AppError(409,"Tento požadavek už probíhá nebo má nejistý výsledek. Ověř n8n; aplikace jej znovu neodešle.");
            return new {input.RequestId,status=reservation.Status,simulated=settings.Local};
        }
        // Once reserved, finish independently of a closed browser connection.
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(65));
        try
        {
            var outcome=await sender.SendAsync(new MailSubmission(input.RequestId,input.Recipient,input.Subject,input.Body),actor,deadline.Token);
            await store.CompleteAsync(input.RequestId,outcome.Status,deadline.Token);
            return new {input.RequestId,outcome.Status,outcome.Simulated};
        }
        catch(Exception exception)
        {
            using var cleanup=new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try {await store.CompleteAsync(input.RequestId,"uncertain",cleanup.Token);}
            catch(Exception) {logger.LogWarning("Could not update delivery coordination record {RequestId}",input.RequestId);}
            if(exception is AppError) throw;
            throw new AppError(exception is OperationCanceledException?504:502,"Odeslání nemá potvrzený výsledek. Před dalším pokusem ověř běh n8n workflow.");
        }
    }
}
