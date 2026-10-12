using Claims.Auditing;
using Claims.Models;
using Claims.Repositories.Interfaces;
using Claims.Services;
using Claims.Services.Interfaces;
using Xunit;

namespace Claims.Tests;

public class DeletionServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteAsync_ExistingRecord_DeletesThenAudits(
        bool isCover)
    {
        var repository = new FakeRepository();
        var auditer = new FakeAuditer(repository.Operations);

        var deleted = await DeleteAsync(
            isCover, repository, auditer, "record-1");

        Assert.True(deleted);
        Assert.Equal("record-1", repository.DeletedId);
        Assert.Equal("record-1", auditer.EntityId);
        Assert.Equal("DELETE", auditer.Operation);
        Assert.Equal(isCover ? "Cover" : "Claim", auditer.EntityType);

        Assert.Equal(
            new[] { "delete", "audit" },
            repository.Operations.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteAsync_MissingRecord_DoesNotDeleteOrAudit(
        bool isCover)
    {
        var repository = new FakeRepository();
        var auditer = new FakeAuditer(repository.Operations);

        var deleted = await DeleteAsync(
            isCover, repository, auditer, "missing-id");

        Assert.False(deleted);
        Assert.Null(repository.DeletedId);
        Assert.Null(auditer.EntityId);
        Assert.Empty(repository.Operations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteAsync_RepositoryFails_DoesNotAudit(
        bool isCover)
    {
        var repository = new FakeRepository
        {
            FailOnDelete = true
        };

        var auditer = new FakeAuditer(repository.Operations);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => DeleteAsync(
                isCover, repository, auditer, "record-1"));

        Assert.Null(auditer.EntityId);
        Assert.DoesNotContain("audit", repository.Operations);
    }

    private static Task<bool> DeleteAsync(
        bool isCover,
        FakeRepository repository,
        FakeAuditer auditer,
        string id)
    {
        if (isCover)
        {
            var service = new CoverService(
                repository,
                new UnusedPremiumCalculator(),
                auditer);

            return service.DeleteAsync(id);
        }

        var claimService = new ClaimService(
            repository,
            repository,
            auditer);

        return claimService.DeleteAsync(id);
    }

    // Explicit interface implementations let this fake support
    // both services while keeping their models separate.
    private sealed class FakeRepository :
        IClaimRepository,
        ICoverRepository
    {
        public List<string> Operations { get; } = new();
        public string? DeletedId { get; private set; }
        public bool FailOnDelete { get; init; }

        Task<Claim?> IClaimRepository.GetByIdAsync(string id)
        {
            Claim? claim = id == "record-1"
                ? new Claim
                {
                    Id = id,
                    CoverId = "cover-1",
                    Name = "Hull damage"
                }
                : null;

            return Task.FromResult(claim);
        }

        Task<Cover?> ICoverRepository.GetByIdAsync(string id)
        {
            Cover? cover = id == "record-1"
                ? new Cover { Id = id }
                : null;

            return Task.FromResult(cover);
        }

        public Task DeleteAsync(string id)
        {
            if (FailOnDelete)
            {
                return Task.FromException(
                    new InvalidOperationException(
                        "Simulated database failure."));
            }

            DeletedId = id;
            Operations.Add("delete");

            return Task.CompletedTask;
        }

        Task<IEnumerable<Claim>> IClaimRepository.GetAllAsync()
        {
            throw new NotSupportedException();
        }

        Task<IEnumerable<Cover>> ICoverRepository.GetAllAsync()
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(Claim claim)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(Cover cover)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeAuditer : IAuditer
    {
        private readonly List<string> _operations;

        public FakeAuditer(List<string> operations)
        {
            _operations = operations;
        }

        public string? EntityId { get; private set; }
        public string? EntityType { get; private set; }
        public string? Operation { get; private set; }

        public void AuditClaim(string id, string httpRequestType)
        {
            Record("Claim", id, httpRequestType);
        }

        public void AuditCover(string id, string httpRequestType)
        {
            Record("Cover", id, httpRequestType);
        }

        private void Record(
            string entityType,
            string id,
            string operation)
        {
            EntityType = entityType;
            EntityId = id;
            Operation = operation;
            _operations.Add("audit");
        }
    }

    private sealed class UnusedPremiumCalculator : IPremiumCalculator
    {
        public decimal ComputePremium(
            DateTime startDate,
            DateTime endDate,
            CoverType coverType)
        {
            throw new NotSupportedException(
                "Deleting a cover should not calculate a premium.");
        }
    }
}
