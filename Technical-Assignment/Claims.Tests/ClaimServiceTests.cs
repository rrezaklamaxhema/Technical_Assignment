using Claims.Auditing;
using Claims.Exceptions;
using Claims.Models;
using Claims.Repositories.Interfaces;
using Claims.Services;
using Xunit;

namespace Claims.Tests;

public class ClaimServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidClaim_SavesAndAudits()
    {
        var repository = new FakeClaimRepository();
        var coverRepository = new FakeCoverRepository();
        var auditer = new FakeAuditer();

        var service = new ClaimService(
            repository, coverRepository, auditer);

        var claim = CreateValidClaim();

        var result = await service.CreateAsync(claim);

        Assert.True(Guid.TryParse(result.Id, out _));
        Assert.Equal(1, repository.AddCalls);
        Assert.Same(result, repository.SavedClaim);

        Assert.Equal("cover-1", coverRepository.RequestedId);

        Assert.Equal(1, auditer.Calls);
        Assert.Equal(result.Id, auditer.EntityId);
        Assert.Equal("POST", auditer.Operation);
    }

    [Fact]
    public async Task CreateAsync_DamageCostExactly100000_AllowsClaim()
    {
        var repository = new FakeClaimRepository();
        var service = new ClaimService(
            repository,
            new FakeCoverRepository(),
            new FakeAuditer());

        var claim = CreateValidClaim();
        claim.DamageCost = 100000m;

        await service.CreateAsync(claim);

        Assert.Equal(1, repository.AddCalls);
        Assert.Equal(100000m, repository.SavedClaim!.DamageCost);
    }

    [Fact]
    public async Task CreateAsync_DamageCostAbove100000_RejectsClaim()
    {
        var claim = CreateValidClaim();
        claim.DamageCost = 100000.01m;

        await AssertRejectedAsync(
            claim,
            "Damage cost cannot exceed 100000.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_MissingCoverId_RejectsClaim(
        string coverId)
    {
        var claim = CreateValidClaim();
        claim.CoverId = coverId;

        await AssertRejectedAsync(
            claim,
            "A cover ID is required.");
    }

    [Fact]
    public async Task CreateAsync_CoverDoesNotExist_RejectsClaim()
    {
        var claim = CreateValidClaim();
        claim.CoverId = "unknown-cover";

        await AssertRejectedAsync(
            claim,
            "The related cover does not exist.");
    }

    [Theory]
    [InlineData(2026, 10, 31)]
    [InlineData(2026, 12, 2)]
    public async Task CreateAsync_DateOutsideCoverPeriod_RejectsClaim(
        int year,
        int month,
        int day)
    {
        var claim = CreateValidClaim();
        claim.Created = new DateTime(year, month, day);

        await AssertRejectedAsync(
            claim,
            "Claim created date must be within the related cover's period.");
    }

    [Theory]
    [InlineData(2026, 11, 1)]
    [InlineData(2026, 12, 1)]
    public async Task CreateAsync_DateOnCoverBoundary_AllowsClaim(
        int year,
        int month,
        int day)
    {
        var repository = new FakeClaimRepository();
        var service = new ClaimService(
            repository,
            new FakeCoverRepository(),
            new FakeAuditer());

        var claim = CreateValidClaim();
        claim.Created = new DateTime(year, month, day);

        await service.CreateAsync(claim);

        Assert.Equal(1, repository.AddCalls);
    }

    private static Claim CreateValidClaim()
    {
        return new Claim
        {
            Id = "client-supplied-id",
            CoverId = "cover-1",
            Created = new DateTime(2026, 11, 15),
            Name = "Hull damage",
            Type = ClaimType.Collision,
            DamageCost = 5000m
        };
    }

    private static async Task AssertRejectedAsync(
        Claim claim,
        string expectedMessage)
    {
        var repository = new FakeClaimRepository();
        var auditer = new FakeAuditer();

        var service = new ClaimService(
            repository,
            new FakeCoverRepository(),
            auditer);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(claim));

        Assert.Equal(expectedMessage, exception.Message);

        // Rejected claims must not be saved or audited.
        Assert.Equal(0, repository.AddCalls);
        Assert.Equal(0, auditer.Calls);
    }

    private sealed class FakeClaimRepository : IClaimRepository
    {
        public Claim? SavedClaim { get; private set; }
        public int AddCalls { get; private set; }

        public Task AddAsync(Claim claim)
        {
            AddCalls++;
            SavedClaim = claim;

            return Task.CompletedTask;
        }

        public Task<IEnumerable<Claim>> GetAllAsync()
        {
            throw new NotSupportedException(
                "This fake only supports claim creation tests.");
        }

        public Task<Claim?> GetByIdAsync(string id)
        {
            throw new NotSupportedException(
                "This fake only supports claim creation tests.");
        }

        public Task DeleteAsync(string id)
        {
            throw new NotSupportedException(
                "This fake only supports claim creation tests.");
        }
    }

    private sealed class FakeCoverRepository : ICoverRepository
    {
        private readonly Cover _cover = new()
        {
            Id = "cover-1",
            StartDate = new DateTime(2026, 11, 1),
            EndDate = new DateTime(2026, 12, 1),
            Type = CoverType.Yacht
        };

        public string? RequestedId { get; private set; }

        public Task<Cover?> GetByIdAsync(string id)
        {
            RequestedId = id;

            Cover? result = id == _cover.Id ? _cover : null;

            return Task.FromResult(result);
        }

        public Task<IEnumerable<Cover>> GetAllAsync()
        {
            throw new NotSupportedException(
                "This fake only supports looking up a cover.");
        }

        public Task AddAsync(Cover cover)
        {
            throw new NotSupportedException(
                "Claim creation should not create a cover.");
        }

        public Task DeleteAsync(string id)
        {
            throw new NotSupportedException(
                "Claim creation should not delete a cover.");
        }
    }

    private sealed class FakeAuditer : IAuditer
    {
        public int Calls { get; private set; }
        public string? EntityId { get; private set; }
        public string? Operation { get; private set; }

        public void AuditClaim(string id, string httpRequestType)
        {
            Calls++;
            EntityId = id;
            Operation = httpRequestType;
        }

        public void AuditCover(string id, string httpRequestType)
        {
            throw new NotSupportedException(
                "Claim creation should not audit a cover.");
        }
    }
}
