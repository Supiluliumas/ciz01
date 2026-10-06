using Backend.Models;
using Microsoft.AspNetCore.HttpOverrides;
namespace Backend;
public static class RequestSecurity
{
    public static ForwardedHeadersOptions ForwardingOptions(bool local)
    {
        var options=new ForwardedHeadersOptions {ForwardedHeaders=ForwardedHeaders.XForwardedProto,ForwardLimit=1};
        if(!local)
        {
            // Platform contract: ingress only via the trusted load balancer.
            // Restore its scheme only; never trust forwarded host or user identity.
            options.KnownIPNetworks.Clear();options.KnownProxies.Clear();
        }
        return options;
    }
    public static void ValidateMutation(HttpRequest request)
    {
        if(!request.Path.StartsWithSegments("/api") || request.Method is "GET" or "HEAD") return;
        if(request.Headers["X-App-Request"]!="1") throw new AppError(403,"Neplatný požadavek aplikace.");
        var origin=request.Headers.Origin.ToString();
        if(origin.Length>0 && (!Uri.TryCreate(origin,UriKind.Absolute,out var uri) || uri.Authority!=request.Host.Value || uri.Scheme!=request.Scheme))
            throw new AppError(403,"Požadavek z jiného webu není povolen.");
    }
}
