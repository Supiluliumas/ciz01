using System.Net;
using Backend;
using Backend.Access;
using Backend.Endpoints;
using Backend.Models;
using Backend.Services;
using Google.Cloud.Firestore;

var builder=WebApplication.CreateBuilder(args);
ConfigEnvLoader.Load(builder.Configuration);
var settings=AppSettings.Load(builder.Configuration);
builder.Services.AddSingleton(settings);
builder.WebHost.ConfigureKestrel(options=>options.Limits.MaxRequestBodySize=32*1024);
if(settings.Local)
{
    builder.Services.AddSingleton<IAppStore,MemoryAppStore>();
    builder.Services.AddSingleton<IDraftGenerator,DemoDraftGenerator>();
    builder.Services.AddSingleton<IMailSender,DemoMailSender>();
    builder.Services.AddSingleton<ITestDataReader,DemoTestData>();
}
else
{
    builder.Services.AddSingleton(_=>FirestoreDb.Create(settings.Project));
    builder.Services.AddSingleton<IAppStore,FirestoreAppStore>();
    builder.Services.AddSingleton<IGoogleToken,GoogleToken>();
    builder.Services.AddHttpClient<GoogleHttp>(client=>client.Timeout=TimeSpan.FromSeconds(60));
    builder.Services.AddTransient<IDraftGenerator,VertexDraftGenerator>();
    builder.Services.AddHttpClient<IMailSender,N8nMailSender>(client=>client.Timeout=TimeSpan.FromSeconds(60))
        .ConfigurePrimaryHttpMessageHandler(()=>new HttpClientHandler{AllowAutoRedirect=false});
    builder.Services.AddTransient<ITestDataReader,BigQueryTestData>();
}
builder.Services.AddTransient<DeliveryService>();
var app=builder.Build();
app.Use(async(context,next)=>
{
    context.Response.Headers["X-Content-Type-Options"]="nosniff";
    context.Response.Headers["Referrer-Policy"]="same-origin";
    context.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    if(context.Request.Path.StartsWithSegments("/api")) context.Response.Headers.CacheControl="no-store";
    try
    {
        if(settings.Local && context.Connection.RemoteIpAddress is {} ip && IPAddress.IsLoopback(ip) &&
            !context.Request.Headers.ContainsKey("X-Goog-Authenticated-User-Email"))
            context.Request.Headers["X-Goog-Authenticated-User-Email"]="accounts.google.com:demo@alza.cz";
        if(context.Request.Path.StartsWithSegments("/api") && context.Request.Method is not ("GET" or "HEAD"))
        {
            if(context.Request.Headers["X-App-Request"]!="1") throw new AppError(403,"Neplatný požadavek aplikace.");
            var origin=context.Request.Headers.Origin.ToString();
            if(origin.Length>0 && (!Uri.TryCreate(origin,UriKind.Absolute,out var uri) || uri.Authority!=context.Request.Host.Value || uri.Scheme!=context.Request.Scheme))
                throw new AppError(403,"Požadavek z jiného webu není povolen.");
        }
        await next(context);
    }
    catch(AppError error) {context.Response.StatusCode=error.Status;await context.Response.WriteAsJsonAsync(new {message=error.Message});}
    catch(OperationCanceledException) when(!context.RequestAborted.IsCancellationRequested)
    {context.Response.StatusCode=504;await context.Response.WriteAsJsonAsync(new {message="Služba neodpověděla včas. Zkus to později."});}
    catch(Exception exception)
    {
        app.Logger.LogError("Request failed with {ExceptionType}; details omitted to protect email content",exception.GetType().Name);
        if(!context.Response.HasStarted){context.Response.StatusCode=500;await context.Response.WriteAsJsonAsync(new {message="Požadavek se nepodařilo dokončit. Ověř nastavení služby."});}
    }
});
app.MapGet("/healthz",()=>Results.Json(new {status="ok"}));
app.MapAppApi(settings);
app.Map("/api/{**path}",()=>Results.Json(new {message="API adresa neexistuje."},statusCode:404));
app.UseDefaultFiles();app.UseStaticFiles();app.MapFallbackToFile("index.html");
app.Run();
public partial class Program {}
