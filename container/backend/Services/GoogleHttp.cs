using System.Net.Http.Headers;
using System.Text.Json;
using Backend.Models;
using Google.Apis.Auth.OAuth2;
namespace Backend.Services;
public interface IGoogleToken {Task<string> TokenAsync(CancellationToken ct);}
public sealed class GoogleToken : IGoogleToken
{
    readonly Lazy<Task<GoogleCredential>> credential=new(()=>GoogleCredential.GetApplicationDefaultAsync());
    public async Task<string> TokenAsync(CancellationToken ct) => await ((ITokenAccess)(await credential.Value).CreateScoped("https://www.googleapis.com/auth/cloud-platform"))
        .GetAccessTokenForRequestAsync(null,ct);
}
public sealed class GoogleHttp(HttpClient client,IGoogleToken token)
{
    public async Task<JsonDocument> JsonAsync(HttpMethod method,string url,object? body,CancellationToken ct)
    {
        using var request=new HttpRequestMessage(method,url);
        request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",await token.TokenAsync(ct));
        if(body is not null) request.Content=JsonContent.Create(body);
        using var response=await client.SendAsync(request,ct);
        if(!response.IsSuccessStatusCode) throw new AppError(502,$"Cloudová služba odmítla požadavek (HTTP {(int)response.StatusCode}). Ověř nastavení a oprávnění projektu.");
        try {return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));}
        catch(JsonException) {throw new AppError(502,"Cloudová služba vrátila neplatnou odpověď.");}
    }
}
