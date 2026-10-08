using Backend.Models;
using Google.Cloud.Firestore;
namespace Backend.Access;

public sealed partial class FirestoreAppStore
{
    DocumentReference RequestDocument(string email) => db.Collection("access_requests").Document(email);
    static AccessRequest? ReadRequest(DocumentSnapshot snapshot) => !snapshot.Exists?null:new(
        snapshot.Id,snapshot.GetValue<string>("status"),snapshot.GetValue<Timestamp>("createdAt").ToDateTimeOffset(),
        snapshot.TryGetValue<Timestamp>("reviewedAt",out var reviewedAt)?reviewedAt.ToDateTimeOffset():null,
        snapshot.TryGetValue<string>("reviewedBy",out var reviewedBy)?reviewedBy:null);

    public Task<AccessRequest?> RequestAsync(string email,bool submit,CancellationToken ct) => db.RunTransactionAsync(async tx =>
    {
        await tx.GetSnapshotAsync(Settings,ct);
        var admin=await tx.GetSnapshotAsync(Member(email,"admin"),ct);
        var user=await tx.GetSnapshotAsync(Member(email,"user"),ct);
        var snapshot=await tx.GetSnapshotAsync(RequestDocument(email),ct);
        var request=ReadRequest(snapshot);
        if(submit && !admin.Exists && !user.Exists && (request is null || request.Status=="approved"))
        {
            request=new AccessRequest(email,"pending",DateTimeOffset.UtcNow);
            tx.Set(snapshot.Reference,new Dictionary<string,object>{{"email",email},{"status","pending"},
                {"createdAt",Timestamp.FromDateTimeOffset(request.CreatedAt)}});
        }
        return request;
    },cancellationToken:ct);

    public Task<AccessRequest[]> RequestsAsync(string actor,CancellationToken ct) => db.RunTransactionAsync(async tx =>
    {
        await tx.GetSnapshotAsync(Settings,ct);
        var admin=await tx.GetSnapshotAsync(Member(actor,"admin"),ct);
        if(!admin.Exists) throw new AppError(403,"Tato akce je dostupná pouze administrátorům.");
        var pending=await tx.GetSnapshotAsync(db.Collection("access_requests").WhereEqualTo("status","pending").Limit(100),ct);
        return pending.Documents.Select(snapshot=>ReadRequest(snapshot)!).OrderBy(request=>request.CreatedAt).ToArray();
    },cancellationToken:ct);

    public Task ReviewAsync(string actor,string email,bool approve,CancellationToken ct) => db.RunTransactionAsync(async tx =>
    {
        await tx.GetSnapshotAsync(Settings,ct);
        var admin=await tx.GetSnapshotAsync(Member(actor,"admin"),ct);
        var request=await tx.GetSnapshotAsync(RequestDocument(email),ct);
        await tx.GetSnapshotAsync(Member(email,"user"),ct);
        if(!admin.Exists) throw new AppError(403,"Tato akce je dostupná pouze administrátorům.");
        if(!request.Exists || request.GetValue<string>("status")!="pending")
            throw new AppError(409,"Žádost už byla vyřízena nebo neexistuje. Obnov seznam žádostí.");
        tx.Update(request.Reference,new Dictionary<string,object>{{"status",approve?"approved":"rejected"},
            {"reviewedAt",FieldValue.ServerTimestamp},{"reviewedBy",actor}});
        if(approve) tx.Set(Member(email,"user"),new Dictionary<string,object>{{"email",email}});
        // Serialize decisions with role/member changes and retire any legacy open mode.
        tx.Update(Settings,new Dictionary<string,object>{{"mode","allowlist"}});
    },cancellationToken:ct);
}
