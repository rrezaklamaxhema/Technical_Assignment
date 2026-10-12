using Claims.Models;

namespace Claims.Services.Interfaces;

public interface IPremiumCalculator
{
    decimal ComputePremium(
        DateTime startDate,
        DateTime endDate,
        CoverType coverType);
}
