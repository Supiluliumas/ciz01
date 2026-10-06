using System.Net;
using Backend;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
namespace Backend.Tests;
public class RequestSecurityTests
{
    static DefaultHttpContext Request(string origin)
    {
        var context=new DefaultHttpContext();
        context.Connection.RemoteIpAddress=IPAddress.Parse("10.1.2.3");
        context.Request.Method="POST";context.Request.Path="/api/send";
        context.Request.Scheme="http";context.Request.Host=new HostString("mailer.vibe.alza.cz");
        context.Request.Headers["Origin"]=origin;context.Request.Headers["X-App-Request"]="1";
        context.Request.Headers["X-Forwarded-Proto"]="https";
        return context;
    }
    [Fact] public async Task CloudProxyHttpsIsRestoredBeforeOriginValidation()
    {
        var context=Request("https://mailer.vibe.alza.cz");
        var forwarding=new ForwardedHeadersMiddleware(_=>Task.CompletedTask,NullLoggerFactory.Instance,Options.Create(RequestSecurity.ForwardingOptions(false)));
        await forwarding.Invoke(context);
        Assert.Equal("https",context.Request.Scheme);
        RequestSecurity.ValidateMutation(context.Request);
    }
    [Fact] public async Task RestoringHttpsDoesNotAllowAnotherOrigin()
    {
        var context=Request("https://other.example.com");
        var forwarding=new ForwardedHeadersMiddleware(_=>Task.CompletedTask,NullLoggerFactory.Instance,Options.Create(RequestSecurity.ForwardingOptions(false)));
        await forwarding.Invoke(context);
        Assert.Throws<AppError>(()=>RequestSecurity.ValidateMutation(context.Request));
    }
    [Fact] public async Task LocalModeDoesNotTrustAnUnknownProxy()
    {
        var context=Request("https://mailer.vibe.alza.cz");
        var forwarding=new ForwardedHeadersMiddleware(_=>Task.CompletedTask,NullLoggerFactory.Instance,Options.Create(RequestSecurity.ForwardingOptions(true)));
        await forwarding.Invoke(context);
        Assert.Equal("http",context.Request.Scheme);
        Assert.Throws<AppError>(()=>RequestSecurity.ValidateMutation(context.Request));
    }
}
