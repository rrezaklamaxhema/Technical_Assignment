using Claims.Auditing;
using Claims.Exceptions;
using Claims.Models;
using Claims.Repositories.Interfaces;
using Claims.Services;
using Claims.Services.Interfaces;
using Xunit;

namespace Claims.Tests;

public class CoverServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidCover_SavesPremiumAndAudits()
    {
        var repository = new FakeCoverRepository();
        var calculator = new FakePremiumCalculator();
        var auditer = new FakeAuditer();

        var service = new CoverService(
            repository, calculator, auditer);

        var startDate = DateTime.UtcNow.Date.AddDays(10);

        var cover = new Cover
        {
            Id = "client-supplied-id",
            StartDate = startDate,
            EndDate = startDate.AddMonths(1),
            Type = CoverType.Yacht,
            Premium = 1m
        };

        var result = await service.CreateAsync(cover);

        Assert.True(Guid.TryParse(result.Id, out _));
        Assert.Equal(25000m, result.Premium);

        Assert.Equal(1, repository.AddCalls);
        Assert.Same(result, repository.SavedCover);

        Assert.Equal(1, calculator.Calls);
        Assert.Equal(cover.StartDate, calculator.StartDate);
        Assert.Equal(cover.EndDate, calculator.EndDate);
        Assert.Equal(cover.Type, calculator.Type);

        Assert.Equal(1, auditer.Calls);
        Assert.Equal(result.Id, auditer.EntityId);
        Assert.Equal("POST", auditer.Operation);
    }

    [Fact]
    public async Task CreateAsync_StartDateInPast_RejectsCover()
    {
        var today = DateTime.UtcNow.Date;

        await AssertRejectedAsync(
            today.AddDays(-1),
            today.AddMonths(1),
            "Cover start date cannot be in the past.");
    }

    [Fact]
    public async Task CreateAsync_EndBeforeStart_RejectsCover()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(10);

        await AssertRejectedAsync(
            startDate,
            startDate.AddDays(-1),
            "Cover end date cannot be before its start date.");
    }

    [Fact]
    public async Task CreateAsync_MoreThanOneYear_RejectsCover()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(10);

        await AssertRejectedAsync(
            startDate,
            startDate.AddYears(1).AddDays(1),
            "The insurance period cannot exceed one year.");
    }

    [Fact]
    public async Task CreateAsync_ExactlyOneYear_AllowsCover()
    {
        var repository = new FakeCoverRepository();
        var service = new CoverService(
            repository,
            new FakePremiumCalculator(),
            new FakeAuditer());

        var startDate = DateTime.UtcNow.Date.AddDays(10);

        var cover = new Cover
        {
            StartDate = startDate,
            EndDate = startDate.AddYears(1),
            Type = CoverType.Yacht
        };

        await service.CreateAsync(cover);

        Assert.Equal(1, repository.AddCalls);
    }

    private static async Task AssertRejectedAsync(
        DateTime startDate,
        DateTime endDate,
        string expectedMessage)
    {
        var repository = new FakeCoverRepository();
        var calculator = new FakePremiumCalculator();
        var auditer = new FakeAuditer();

        var service = new CoverService(
            repository, calculator, auditer);

        var cover = new Cover
        {
            StartDate = startDate,
            EndDate = endDate,
            Type = CoverType.Yacht
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(cover));

        Assert.Equal(expectedMessage, exception.Message);

        // Invalid input must not cause downstream operations.
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, calculator.Calls);
        Assert.Equal(0, auditer.Calls);
    }

    private sealed class FakeCoverRepository : ICoverRepository
    {
        public Cover? SavedCover { get; private set; }
        public int AddCalls { get; private set; }

        public Task AddAsync(Cover cover)
        {
            AddCalls++;
            SavedCover = cover;

            return Task.CompletedTask;
        }

        public Task<IEnumerable<Cover>> GetAllAsync()
        {
            throw new NotSupportedException(
                "This fake only supports cover creation tests.");
        }

        public Task<Cover?> GetByIdAsync(string id)
        {
            throw new NotSupportedException(
                "This fake only supports cover creation tests.");
        }

        public Task DeleteAsync(string id)
        {
            throw new NotSupportedException(
                "This fake only supports cover creation tests.");
        }
    }

    private sealed class FakePremiumCalculator : IPremiumCalculator
    {
        public int Calls { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public CoverType Type { get; private set; }

        public decimal ComputePremium(
            DateTime startDate,
            DateTime endDate,
            CoverType coverType)
        {
            Calls++;
            StartDate = startDate;
            EndDate = endDate;
            Type = coverType;

            return 25000m;
        }
    }

    private sealed class FakeAuditer : IAuditer
    {
        public int Calls { get; private set; }
        public string? EntityId { get; private set; }
        public string? Operation { get; private set; }

        public void AuditCover(string id, string httpRequestType)
        {
            Calls++;
            EntityId = id;
            Operation = httpRequestType;
        }

        public void AuditClaim(string id, string httpRequestType)
        {
            throw new NotSupportedException(
                "Cover creation should not audit a claim.");
        }
    }
}
