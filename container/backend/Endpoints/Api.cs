using Backend.Access;
using Backend.Models;
using Backend.Services;
namespace Backend.Endpoints;
public static class Api
{
    static string Actor(HttpContext context) => (string)context.Items["email"]!;
    public static void MapAppApi(this WebApplication app,AppSettings settings)
    {
        var api=app.MapGroup("/api").AddEndpointFilter(async (context,next)=>
        {
            var http=context.HttpContext;
            var raw=http.Request.Headers["X-Goog-Authenticated-User-Email"].FirstOrDefault();
            if(string.IsNullOrWhiteSpace(raw)) throw new AppError(401,"Chybí firemní přihlášení.");
            string email;
            try {email=Input.CorporateEmail(raw.Contains(':')?raw.Split(':',2)[1]:raw);}
            catch(AppError) {throw new AppError(401,"Neplatné firemní přihlášení.");}
            var decision=await http.RequestServices.GetRequiredService<IAppStore>().AccessAsync(email,http.RequestAborted);
            var selfService=http.Request.Path=="/api/access-request" && http.Request.Method is "GET" or "POST";
            if(!decision.Allowed && !selfService) throw new AppError(403,"Nemáš přístup do aplikace. Požádej administrátora o přidání.");
            if(http.Request.Path.StartsWithSegments("/api/admin") && !decision.IsAdmin) throw new AppError(403,"Tato část je dostupná pouze administrátorům.");
            http.Items["email"]=email;http.Items["admin"]=decision.IsAdmin;
            return await next(context);
        });
        api.Map("/{**path}",()=>Results.Json(new {message="API adresa neexistuje."},statusCode:404));
        async Task<object> OwnRequest(HttpContext context,IAppStore store,bool submit,CancellationToken ct)
        {
            var email=Actor(context);
            var request=await store.RequestAsync(email,submit,ct);
            var decision=await store.AccessAsync(email,ct);
            var status=decision.Allowed?"approved":request?.Status=="approved"?"revoked":request?.Status??"none";
            return new {email,status,allowed=decision.Allowed,isAdmin=decision.IsAdmin,localMode=settings.Local};
        }
        api.MapGet("/access-request",async(HttpContext context,IAppStore store,CancellationToken ct)=>await OwnRequest(context,store,false,ct));
        api.MapPost("/access-request",async(HttpContext context,IAppStore store,CancellationToken ct)=>await OwnRequest(context,store,true,ct));
        api.MapGet("/admin/requests",async(HttpContext context,IAppStore store,CancellationToken ct)=>await store.RequestsAsync(Actor(context),ct));
        api.MapPost("/admin/requests",async(ReviewRequest input,HttpContext context,IAppStore store,CancellationToken ct)=>{
            var decision=Input.Choice(input.Decision,"approve","reject");
            await store.ReviewAsync(Actor(context),Input.CorporateEmail(input.Email),decision=="approve",ct);return Results.Ok();});
        api.MapGet("/me",(HttpContext context)=>new {email=Actor(context),isAdmin=(bool)context.Items["admin"]!,localMode=settings.Local});
        api.MapGet("/status",()=>new {localMode=settings.Local,ai=settings.Local?"simulation":"configured",email=settings.Local?"simulation":"configured",testData=settings.Local?"simulation":"configured",secret=settings.Local?"not-used":"configured"});
        api.MapPost("/drafts",async(DraftRequest input,IDraftGenerator generator,CancellationToken ct)=>await generator.GenerateAsync(input.Validated(),ct));
        api.MapPost("/send",async(SendRequest input,HttpContext context,DeliveryService service,CancellationToken ct)=>await service.SendAsync(input.Validated(),Actor(context),ct));
        api.MapGet("/test-data",async(ITestDataReader reader,CancellationToken ct)=>await reader.ReadAsync(ct));
        api.MapGet("/admin/access",async(HttpContext context,IAppStore store,CancellationToken ct)=>await store.StateAsync(Actor(context),ct));
        api.MapPost("/admin/members",async(MemberRequest input,HttpContext context,IAppStore store,CancellationToken ct)=>{
            await store.ChangeMemberAsync(Actor(context),Input.CorporateEmail(input.Email),Input.Choice(input.Role,"admin","user"),false,ct);return Results.Ok();});
        api.MapDelete("/admin/members",async(string email,string role,HttpContext context,IAppStore store,CancellationToken ct)=>{
            await store.ChangeMemberAsync(Actor(context),Input.CorporateEmail(email),Input.Choice(role,"admin","user"),true,ct);return Results.Ok();});
        api.MapPut("/admin/mode",async(ModeRequest input,HttpContext context,IAppStore store,CancellationToken ct)=>{
            await store.ChangeModeAsync(Actor(context),Input.Choice(input.Mode,"allowlist"),ct);return Results.Ok();});
    }
}
