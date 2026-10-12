using Claims.Models;

namespace Claims.Repositories.Interfaces;

public interface IClaimRepository
{
    Task<IEnumerable<Claim>> GetAllAsync();
    Task<Claim?> GetByIdAsync(string id);
    Task AddAsync(Claim claim);
    Task DeleteAsync(string id);
}
