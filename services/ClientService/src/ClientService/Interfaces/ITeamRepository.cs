using ClientService.Entities;

namespace ClientService.Interfaces;

public interface ITeamRepository
{
    Task AddAsync(Team team, CancellationToken ct);
    Task<Team?> GetByIdAsync(Guid id, CancellationToken ct);
}
