using Backend.Models;
namespace Backend.Access;
public sealed class MemoryAppStore(AppSettings settings) : IAppStore
{
    readonly object gate=new();
    readonly HashSet<string> admins=[];
    readonly HashSet<string> users=[];
    readonly Dictionary<Guid,(string Actor,string Fingerprint,string Status)> sends=[];
    readonly Dictionary<string,AccessRequest> requests=[];
    bool initialized;
    public Task<AccessDecision> AccessAsync(string email,CancellationToken ct)
    {
        lock(gate)
        {
            if(!initialized)
            {
                if(settings.BootstrapAdminEmail.Length>0 && !email.Equals(settings.BootstrapAdminEmail,StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(new AccessDecision(false,false));
                admins.Add(email); initialized=true;
            }
            return Task.FromResult(new AccessDecision(admins.Contains(email)||users.Contains(email),admins.Contains(email)));
        }
    }
    public Task<AccessState> StateAsync(string actor,CancellationToken ct)
    {lock(gate){RequireAdmin(actor);return Task.FromResult(new AccessState(admins.Order().ToArray(),users.Order().ToArray(),"allowlist"));}}
    void RequireAdmin(string actor) {if(!admins.Contains(actor)) throw new AppError(403,"Tato akce je dostupná pouze administrátorům.");}
    public Task ChangeMemberAsync(string actor,string email,string role,bool remove,CancellationToken ct)
    {
        lock(gate)
        {
            RequireAdmin(actor); var collection=role=="admin"?admins:users;
            if(remove && role=="admin" && admins.Contains(email) && admins.Count==1) throw new AppError(409,"Posledního administrátora nelze odebrat.");
            if(remove) collection.Remove(email); else collection.Add(email);
        }
        return Task.CompletedTask;
    }
    public Task ChangeModeAsync(string actor,string value,CancellationToken ct)
    {lock(gate){RequireAdmin(actor);Input.Choice(value,"allowlist");} return Task.CompletedTask;}
    public Task<AccessRequest?> RequestAsync(string email,bool submit,CancellationToken ct)
    {
        lock(gate)
        {
            requests.TryGetValue(email,out var request);
            if(submit && !admins.Contains(email) && !users.Contains(email) &&
                (request is null || request.Status=="approved"))
                requests[email]=request=new AccessRequest(email,"pending",DateTimeOffset.UtcNow);
            return Task.FromResult(request);
        }
    }
    public Task<AccessRequest[]> RequestsAsync(string actor,CancellationToken ct)
    {
        lock(gate){RequireAdmin(actor);return Task.FromResult(requests.Values.Where(r=>r.Status=="pending").OrderBy(r=>r.CreatedAt).Take(100).ToArray());}
    }
    public Task ReviewAsync(string actor,string email,bool approve,CancellationToken ct)
    {
        lock(gate)
        {
            RequireAdmin(actor);
            if(!requests.TryGetValue(email,out var request) || request.Status!="pending")
                throw new AppError(409,"Žádost už byla vyřízena nebo neexistuje. Obnov seznam žádostí.");
            requests[email]=request with {Status=approve?"approved":"rejected",ReviewedAt=DateTimeOffset.UtcNow,ReviewedBy=actor};
            if(approve) users.Add(email);
        }
        return Task.CompletedTask;
    }
    public Task<SendReservation> ReserveAsync(Guid id,string actor,string fingerprint,CancellationToken ct)
    {
        lock(gate)
        {
            if(sends.TryGetValue(id,out var previous))
            {
                if(previous.Actor!=actor || previous.Fingerprint!=fingerprint) throw new AppError(409,"Identifikátor odeslání už patří jinému požadavku.");
                return Task.FromResult(new SendReservation(false,previous.Status));
            }
            sends.Add(id,(actor,fingerprint,"pending")); return Task.FromResult(new SendReservation(true,"pending"));
        }
    }
    public Task CompleteAsync(Guid id,string status,CancellationToken ct)
    {lock(gate){var prior=sends[id];sends[id]=(prior.Actor,prior.Fingerprint,status);}return Task.CompletedTask;}
}
