using Backend.Models;
namespace Backend.Access;
public interface IAppStore
{
    Task<AccessDecision> AccessAsync(string email,CancellationToken ct);
    Task<AccessState> StateAsync(CancellationToken ct);
    Task ChangeMemberAsync(string actor,string email,string role,bool remove,CancellationToken ct);
    Task ChangeModeAsync(string actor,string mode,CancellationToken ct);
    Task<SendReservation> ReserveAsync(Guid id,string actor,string fingerprint,CancellationToken ct);
    Task CompleteAsync(Guid id,string status,CancellationToken ct);
}
