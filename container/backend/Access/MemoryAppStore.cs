using Backend.Models;
namespace Backend.Access;
public sealed class MemoryAppStore : IAppStore
{
    readonly object gate=new();
    readonly HashSet<string> admins=[];
    readonly HashSet<string> users=[];
    readonly Dictionary<Guid,(string Actor,string Fingerprint,string Status)> sends=[];
    string mode="allowlist";
    bool initialized;
    public Task<AccessDecision> AccessAsync(string email,CancellationToken ct)
    {
        lock(gate)
        {
            if(!initialized) {admins.Add(email); initialized=true;}
            return Task.FromResult(new AccessDecision(admins.Contains(email)||users.Contains(email)||mode=="everyone",admins.Contains(email)));
        }
    }
    public Task<AccessState> StateAsync(CancellationToken ct)
    {lock(gate) return Task.FromResult(new AccessState(admins.Order().ToArray(),users.Order().ToArray(),mode));}
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
    {lock(gate){RequireAdmin(actor);mode=value;} return Task.CompletedTask;}
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
