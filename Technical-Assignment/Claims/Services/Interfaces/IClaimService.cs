using Claims.Models;

namespace Claims.Services.Interfaces;

public interface IClaimService
{
    Task<IEnumerable<Claim>> GetAllAsync();
    Task<Claim?> GetByIdAsync(string id);
    Task<Claim> CreateAsync(Claim claim);
    Task<bool> DeleteAsync(string id);
}