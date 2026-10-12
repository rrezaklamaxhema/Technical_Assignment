using Claims.Controllers;
using Claims.Exceptions;
using Claims.Models;
using Claims.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Claims.Tests;

public class ClaimsControllerUnitTests
{
    [Fact]
    public async Task GetAsync_Returns200WithClaims()
    {
        var service = new FakeClaimService();
        var controller = new ClaimsController(service);

        var response = await controller.GetAsync();

        var result = Assert.IsType<OkObjectResult>(response.Result);
        var claims = Assert.IsAssignableFrom<IEnumerable<Claim>>(
            result.Value);

        var claim = Assert.Single(claims);

        Assert.Equal("claim-1", claim.Id);
    }

    [Fact]
    public async Task GetAsync_ExistingId_Returns200WithClaim()
    {
        var service = new FakeClaimService();
        var controller = new ClaimsController(service);

        var response = await controller.GetAsync("claim-1");

        var result = Assert.IsType<OkObjectResult>(response.Result);
        var claim = Assert.IsType<Claim>(result.Value);

        Assert.Equal("claim-1", claim.Id);
        Assert.Equal("claim-1", service.RequestedId);
    }

    [Fact]
    public async Task GetAsync_MissingId_Returns404()
    {
        var service = new FakeClaimService();
        var controller = new ClaimsController(service);

        var response = await controller.GetAsync("missing-id");

        Assert.IsType<NotFoundResult>(response.Result);
        Assert.Equal("missing-id", service.RequestedId);
    }

    [Fact]
    public async Task CreateAsync_ValidClaim_Returns200WithCreatedClaim()
    {
        var service = new FakeClaimService();
        var controller = new ClaimsController(service);
        var request = CreateRequest();

        var response = await controller.CreateAsync(request);

        var result = Assert.IsType<OkObjectResult>(response.Result);
        var createdClaim = Assert.IsType<Claim>(result.Value);

        Assert.Same(request, service.ReceivedClaim);
        Assert.Same(service.CreatedClaim, createdClaim);
        Assert.Equal("created-claim", createdClaim.Id);
    }

    [Fact]
    public async Task CreateAsync_ValidationFails_Returns400()
    {
        var service = new FakeClaimService
        {
            CreateException = new ValidationException(
                "Damage cost cannot exceed 100000.")
        };

        var controller = new ClaimsController(service);

        var response = await controller.CreateAsync(CreateRequest());

        var result = Assert.IsType<BadRequestObjectResult>(
            response.Result);

        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task CreateAsync_UnexpectedFailure_IsNotConvertedTo400()
    {
        var expectedException = new InvalidOperationException(
            "Simulated database failure.");

        var service = new FakeClaimService
        {
            CreateException = expectedException
        };

        var controller = new ClaimsController(service);

        var actualException =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.CreateAsync(CreateRequest()));

        Assert.Same(expectedException, actualException);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_Returns204()
    {
        var service = new FakeClaimService();
        var controller = new ClaimsController(service);

        var response = await controller.DeleteAsync("claim-1");

        Assert.IsType<NoContentResult>(response);
        Assert.Equal("claim-1", service.DeletedId);
    }

    [Fact]
    public async Task DeleteAsync_MissingId_Returns404()
    {
        var service = new FakeClaimService();
        var controller = new ClaimsController(service);

        var response = await controller.DeleteAsync("missing-id");

        Assert.IsType<NotFoundResult>(response);
        Assert.Equal("missing-id", service.DeletedId);
    }

    private static Claim CreateRequest()
    {
        return new Claim
        {
            Id = "",
            CoverId = "cover-1",
            Name = "Hull damage",
            Created = new DateTime(2026, 11, 15),
            Type = ClaimType.Collision,
            DamageCost = 5000m
        };
    }

    private sealed class FakeClaimService : IClaimService
    {
        private readonly Claim _existingClaim = new()
        {
            Id = "claim-1",
            CoverId = "cover-1",
            Name = "Existing claim",
            Created = new DateTime(2026, 11, 15),
            Type = ClaimType.Collision,
            DamageCost = 5000m
        };

        public Claim CreatedClaim { get; } = new()
        {
            Id = "created-claim",
            CoverId = "cover-1",
            Name = "Hull damage",
            Created = new DateTime(2026, 11, 15),
            Type = ClaimType.Collision,
            DamageCost = 5000m
        };

        public string? RequestedId { get; private set; }
        public string? DeletedId { get; private set; }
        public Claim? ReceivedClaim { get; private set; }
        public Exception? CreateException { get; init; }

        public Task<IEnumerable<Claim>> GetAllAsync()
        {
            IEnumerable<Claim> claims = new[] { _existingClaim };

            return Task.FromResult(claims);
        }

        public Task<Claim?> GetByIdAsync(string id)
        {
            RequestedId = id;

            Claim? claim = id == _existingClaim.Id
                ? _existingClaim
                : null;

            return Task.FromResult(claim);
        }

        public Task<Claim> CreateAsync(Claim claim)
        {
            ReceivedClaim = claim;

            if (CreateException is not null)
            {
                return Task.FromException<Claim>(CreateException);
            }

            return Task.FromResult(CreatedClaim);
        }

        public Task<bool> DeleteAsync(string id)
        {
            DeletedId = id;

            return Task.FromResult(id == _existingClaim.Id);
        }
    }
}
