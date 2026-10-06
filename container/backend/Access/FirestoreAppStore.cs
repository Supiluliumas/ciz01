using Backend.Models;
using Google.Cloud.Firestore;
namespace Backend.Access;
public sealed class FirestoreAppStore(FirestoreDb db) : IAppStore
{
    DocumentReference Settings => db.Collection("settings").Document("access");
    DocumentReference Member(string email,string role) => db.Collection(role=="admin"?"admins":"allowed_users").Document(email);
    public Task<AccessDecision> AccessAsync(string email,CancellationToken ct) => db.RunTransactionAsync(async tx =>
    {
        var settings=await tx.GetSnapshotAsync(Settings,ct);
        var admin=await tx.GetSnapshotAsync(Member(email,"admin"),ct);
        var user=await tx.GetSnapshotAsync(Member(email,"user"),ct);
        if(!settings.Exists)
        {
            var existing=await tx.GetSnapshotAsync(db.Collection("admins"),ct);
            var first=existing.Count==0;
            if(first) tx.Set(Member(email,"admin"),new Dictionary<string,object>{{"email",email}});
            tx.Set(Settings,new Dictionary<string,object>{{"mode","allowlist"},{"initialized",true},{"adminCount",first?1:existing.Count}});
            return new AccessDecision(first||admin.Exists||user.Exists,first||admin.Exists);
        }
        return new AccessDecision(admin.Exists||user.Exists||settings.GetValue<string>("mode")=="everyone",admin.Exists);
    },cancellationToken:ct);
    public Task<AccessState> StateAsync(CancellationToken ct) => db.RunTransactionAsync(async tx =>
    {
        var settings=await tx.GetSnapshotAsync(Settings,ct);
        var admins=await tx.GetSnapshotAsync(db.Collection("admins"),ct);
        var users=await tx.GetSnapshotAsync(db.Collection("allowed_users"),ct);
        return new AccessState(admins.Documents.Select(d=>d.Id).Order().ToArray(),users.Documents.Select(d=>d.Id).Order().ToArray(),settings.GetValue<string>("mode"));
    },cancellationToken:ct);
    public Task ChangeMemberAsync(string actor,string email,string role,bool remove,CancellationToken ct) => db.RunTransactionAsync(async tx =>
    {
        var settings=await tx.GetSnapshotAsync(Settings,ct);
        var actorDoc=await tx.GetSnapshotAsync(Member(actor,"admin"),ct);
        var target=await tx.GetSnapshotAsync(Member(email,role),ct);
        if(!actorDoc.Exists) throw new AppError(403,"Tato akce je dostupná pouze administrátorům.");
        var count=settings.GetValue<long>("adminCount");
        if(role=="admin" && remove && target.Exists && count<=1) throw new AppError(409,"Posledního administrátora nelze odebrat.");
        if(remove) tx.Delete(target.Reference); else tx.Set(target.Reference,new Dictionary<string,object>{{"email",email}});
        if(role=="admin") count+=remove?(target.Exists?-1:0):(target.Exists?0:1);
        tx.Update(Settings,new Dictionary<string,object>{{"adminCount",count}});
    },cancellationToken:ct);
    public Task ChangeModeAsync(string actor,string mode,CancellationToken ct) => db.RunTransactionAsync(async tx =>
    {
        await tx.GetSnapshotAsync(Settings,ct);
        var admin=await tx.GetSnapshotAsync(Member(actor,"admin"),ct);
        if(!admin.Exists) throw new AppError(403,"Tato akce je dostupná pouze administrátorům.");
        tx.Update(Settings,new Dictionary<string,object>{{"mode",mode}});
    },cancellationToken:ct);
    public Task<SendReservation> ReserveAsync(Guid id,string actor,string fingerprint,CancellationToken ct) => db.RunTransactionAsync(async tx =>
    {
        var reference=db.Collection("send_attempts").Document(id.ToString());
        var snapshot=await tx.GetSnapshotAsync(reference,ct);
        if(snapshot.Exists)
        {
            if(snapshot.GetValue<string>("ownerEmail")!=actor || snapshot.GetValue<string>("fingerprint")!=fingerprint)
                throw new AppError(409,"Identifikátor odeslání už patří jinému požadavku.");
            return new SendReservation(false,snapshot.GetValue<string>("status"));
        }
        tx.Create(reference,new Dictionary<string,object>{{"ownerEmail",actor},{"fingerprint",fingerprint},{"status","pending"},{"createdAt",FieldValue.ServerTimestamp}});
        return new SendReservation(true,"pending");
    },cancellationToken:ct);
    public async Task CompleteAsync(Guid id,string status,CancellationToken ct) =>
        await db.Collection("send_attempts").Document(id.ToString()).UpdateAsync("status",status,cancellationToken:ct);
}
