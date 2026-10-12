using Claims.Auditing;
using Claims.Exceptions;
using Claims.Models;
using Claims.Repositories.Interfaces;
using Claims.Services.Interfaces;

namespace Claims.Services;

public class CoverService : ICoverService
{
    private readonly ICoverRepository _coverRepository;
    private readonly IPremiumCalculator _premiumCalculator;
    private readonly IAuditer _auditer;

    public CoverService(
        ICoverRepository coverRepository,
        IPremiumCalculator premiumCalculator,
        IAuditer auditer)
    {
        _coverRepository = coverRepository;
        _premiumCalculator = premiumCalculator;
        _auditer = auditer;
    }

    public async Task<IEnumerable<Cover>> GetAllAsync()
    {
        return await _coverRepository.GetAllAsync();
    }

    public async Task<Cover?> GetByIdAsync(string id)
    {
        return await _coverRepository.GetByIdAsync(id);
    }

    public async Task<Cover> CreateAsync(Cover cover)
    {
        ValidateCover(cover);

        cover.Id = Guid.NewGuid().ToString();

        cover.Premium = _premiumCalculator.ComputePremium(
            cover.StartDate,
            cover.EndDate,
            cover.Type);

        await _coverRepository.AddAsync(cover);
        _auditer.AuditCover(cover.Id, "POST");

        return cover;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var cover = await _coverRepository.GetByIdAsync(id);

        if (cover is null)
        {
            return false;
        }

        await _coverRepository.DeleteAsync(id);
        _auditer.AuditCover(id, "DELETE");

        return true;
    }

    private static void ValidateCover(Cover cover)
    {
        var startDate = cover.StartDate.Date;
        var endDate = cover.EndDate.Date;
        var today = DateTime.UtcNow.Date;

        if (startDate < today)
        {
            throw new ValidationException(
                "Cover start date cannot be in the past.");
        }

        if (endDate < startDate)
        {
            throw new ValidationException(
                "Cover end date cannot be before its start date.");
        }

        if (endDate > startDate.AddYears(1))
        {
            throw new ValidationException(
                "The insurance period cannot exceed one year.");
        }
    }
}
