using ClientService.Data;
using ClientService.Entities;
using ClientService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientService.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly ClientDbContext _context;

    public TeamRepository(ClientDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Team team, CancellationToken ct)
    {
        await _context.Teams.AddAsync(team, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<Team?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Teams.FindAsync([id], cancellationToken: ct);
    }
}
