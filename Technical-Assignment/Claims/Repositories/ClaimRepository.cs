using Claims.Data;
using Claims.Models;
using Claims.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Claims.Repositories;

public class ClaimRepository : IClaimRepository
{
    private readonly ClaimsContext _context;

    public ClaimRepository(ClaimsContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Claim>> GetAllAsync()
    {
        return await _context.Claims.ToListAsync();
    }

    public async Task<Claim?> GetByIdAsync(string id)
    {
        return await _context.Claims
            .SingleOrDefaultAsync(claim => claim.Id == id);
    }

    public async Task AddAsync(Claim claim)
    {
        _context.Claims.Add(claim);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var claim = await GetByIdAsync(id);

        if (claim is null)
        {
            return;
        }

        _context.Claims.Remove(claim);
        await _context.SaveChangesAsync();
    }
}
