using Claims.Auditing;
using Claims.Exceptions;
using Claims.Models;
using Claims.Repositories.Interfaces;
using Claims.Services.Interfaces;

namespace Claims.Services;

public class ClaimService : IClaimService
{
    private readonly IClaimRepository _claimRepository;
    private readonly ICoverRepository _coverRepository;
    private readonly IAuditer _auditer;

    public ClaimService(
        IClaimRepository claimRepository,
        ICoverRepository coverRepository,
        IAuditer auditer)
    {
        _claimRepository = claimRepository;
        _coverRepository = coverRepository;
        _auditer = auditer;
    }

    public async Task<IEnumerable<Claim>> GetAllAsync()
    {
        return await _claimRepository.GetAllAsync();
    }

    public async Task<Claim?> GetByIdAsync(string id)
    {
        return await _claimRepository.GetByIdAsync(id);
    }

    public async Task<Claim> CreateAsync(Claim claim)
    {
        await ValidateClaimAsync(claim);

        claim.Id = Guid.NewGuid().ToString();

        await _claimRepository.AddAsync(claim);
        _auditer.AuditClaim(claim.Id, "POST");

        return claim;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var claim = await _claimRepository.GetByIdAsync(id);

        if (claim is null)
        {
            return false;
        }

        await _claimRepository.DeleteAsync(id);
        _auditer.AuditClaim(id, "DELETE");

        return true;
    }

    private async Task ValidateClaimAsync(Claim claim)
    {
        if (claim.DamageCost > 100000m)
        {
            throw new ValidationException(
                "Damage cost cannot exceed 100000.");
        }

        if (string.IsNullOrWhiteSpace(claim.CoverId))
        {
            throw new ValidationException(
                "A cover ID is required.");
        }

        var cover = await _coverRepository.GetByIdAsync(claim.CoverId);

        if (cover is null)
        {
            throw new ValidationException(
                "The related cover does not exist.");
        }

        var createdDate = claim.Created.Date;

        if (createdDate < cover.StartDate.Date ||
            createdDate > cover.EndDate.Date)
        {
            throw new ValidationException(
                "Claim created date must be within the related cover's period.");
        }
    }
}