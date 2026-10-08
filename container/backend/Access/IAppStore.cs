using Backend.Models;
namespace Backend.Access;
public interface IAppStore
{
    Task<AccessDecision> AccessAsync(string email,CancellationToken ct);
    Task<AccessState> StateAsync(string actor,CancellationToken ct);
    Task ChangeMemberAsync(string actor,string email,string role,bool remove,CancellationToken ct);
    Task ChangeModeAsync(string actor,string mode,CancellationToken ct);
    Task<AccessRequest?> RequestAsync(string email,bool submit,CancellationToken ct);
    Task<AccessRequest[]> RequestsAsync(string actor,CancellationToken ct);
    Task ReviewAsync(string actor,string email,bool approve,CancellationToken ct);
    Task<SendReservation> ReserveAsync(Guid id,string actor,string fingerprint,CancellationToken ct);
    Task CompleteAsync(Guid id,string status,CancellationToken ct);
}
