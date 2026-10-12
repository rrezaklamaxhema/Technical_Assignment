using Claims.Models;

namespace Claims.Services.Interfaces;

public interface ICoverService
{
    Task<IEnumerable<Cover>> GetAllAsync();
    Task<Cover?> GetByIdAsync(string id);
    Task<Cover> CreateAsync(Cover cover);
    Task<bool> DeleteAsync(string id);
}
