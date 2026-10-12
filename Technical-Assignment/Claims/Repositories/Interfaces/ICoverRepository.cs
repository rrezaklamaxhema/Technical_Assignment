using Claims.Models;

namespace Claims.Repositories.Interfaces;

public interface ICoverRepository
{
    Task<IEnumerable<Cover>> GetAllAsync();
    Task<Cover?> GetByIdAsync(string id);
    Task AddAsync(Cover cover);
    Task DeleteAsync(string id);
}
