using Claims.Controllers;
using Claims.Exceptions;
using Claims.Models;
using Claims.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Claims.Tests;

public class CoversControllerUnitTests
{
    [Fact]
    public async Task GetAsync_Returns200WithCovers()
    {
        var controller = CreateController(new FakeCoverService());

        var response = await controller.GetAsync();

        var result = Assert.IsType<OkObjectResult>(response.Result);
        var covers = Assert.IsAssignableFrom<IEnumerable<Cover>>(
            result.Value);

        var cover = Assert.Single(covers);
        Assert.Equal("cover-1", cover.Id);
    }

    [Fact]
    public async Task GetAsync_ExistingId_Returns200WithCover()
    {
        var service = new FakeCoverService();
        var controller = CreateController(service);

        var response = await controller.GetAsync("cover-1");

        var result = Assert.IsType<OkObjectResult>(response.Result);
        var cover = Assert.IsType<Cover>(result.Value);

        Assert.Equal("cover-1", cover.Id);
        Assert.Equal("cover-1", service.RequestedId);
    }

    [Fact]
    public async Task GetAsync_MissingId_Returns404()
    {
        var service = new FakeCoverService();
        var controller = CreateController(service);

        var response = await controller.GetAsync("missing-id");

        Assert.IsType<NotFoundResult>(response.Result);
        Assert.Equal("missing-id", service.RequestedId);
    }

    [Fact]
    public async Task CreateAsync_ValidCover_Returns200WithCreatedCover()
    {
        var service = new FakeCoverService();
        var controller = CreateController(service);
        var request = CreateRequest();

        var response = await controller.CreateAsync(request);

        var result = Assert.IsType<OkObjectResult>(response.Result);
        var cover = Assert.IsType<Cover>(result.Value);

        Assert.Same(request, service.ReceivedCover);
        Assert.Same(service.CreatedCover, cover);
        Assert.Equal("created-cover", cover.Id);
        Assert.Equal(25000m, cover.Premium);
    }

    [Fact]
    public async Task CreateAsync_ValidationFails_Returns400()
    {
        var service = new FakeCoverService
        {
            CreateException = new ValidationException(
                "Cover start date cannot be in the past.")
        };

        var controller = CreateController(service);

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

        var service = new FakeCoverService
        {
            CreateException = expectedException
        };

        var controller = CreateController(service);

        var actualException =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.CreateAsync(CreateRequest()));

        Assert.Same(expectedException, actualException);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_Returns204()
    {
        var service = new FakeCoverService();
        var controller = CreateController(service);

        var response = await controller.DeleteAsync("cover-1");

        Assert.IsType<NoContentResult>(response);
        Assert.Equal("cover-1", service.DeletedId);
    }

    [Fact]
    public async Task DeleteAsync_MissingId_Returns404()
    {
        var service = new FakeCoverService();
        var controller = CreateController(service);

        var response = await controller.DeleteAsync("missing-id");

        Assert.IsType<NotFoundResult>(response);
        Assert.Equal("missing-id", service.DeletedId);
    }

    [Fact]
    public void ComputePremium_Returns200WithCalculatorResult()
    {
        var calculator = new FakePremiumCalculator();

        var controller = new CoversController(
            new FakeCoverService(),
            calculator);

        var startDate = new DateTime(2026, 11, 1);
        var endDate = new DateTime(2026, 12, 1);

        var response = controller.ComputePremium(
            startDate,
            endDate,
            CoverType.Yacht);

        var result = Assert.IsType<OkObjectResult>(response.Result);
        var premium = Assert.IsType<decimal>(result.Value);

        Assert.Equal(25000m, premium);
        Assert.Equal(1, calculator.Calls);
        Assert.Equal(startDate, calculator.StartDate);
        Assert.Equal(endDate, calculator.EndDate);
        Assert.Equal(CoverType.Yacht, calculator.Type);
    }

    private static CoversController CreateController(
        FakeCoverService service)
    {
        return new CoversController(
            service,
            new FakePremiumCalculator());
    }

    private static Cover CreateRequest()
    {
        return new Cover
        {
            Id = "",
            StartDate = new DateTime(2026, 11, 1),
            EndDate = new DateTime(2026, 12, 1),
            Type = CoverType.Yacht,
            Premium = 0m
        };
    }

    private sealed class FakeCoverService : ICoverService
    {
        private readonly Cover _existingCover = new()
        {
            Id = "cover-1",
            StartDate = new DateTime(2026, 11, 1),
            EndDate = new DateTime(2026, 12, 1),
            Type = CoverType.Yacht,
            Premium = 25000m
        };

        public Cover CreatedCover { get; } = new()
        {
            Id = "created-cover",
            StartDate = new DateTime(2026, 11, 1),
            EndDate = new DateTime(2026, 12, 1),
            Type = CoverType.Yacht,
            Premium = 25000m
        };

        public string? RequestedId { get; private set; }
        public string? DeletedId { get; private set; }
        public Cover? ReceivedCover { get; private set; }
        public Exception? CreateException { get; init; }

        public Task<IEnumerable<Cover>> GetAllAsync()
        {
            IEnumerable<Cover> covers = new[] { _existingCover };

            return Task.FromResult(covers);
        }

        public Task<Cover?> GetByIdAsync(string id)
        {
            RequestedId = id;

            Cover? cover = id == _existingCover.Id
                ? _existingCover
                : null;

            return Task.FromResult(cover);
        }

        public Task<Cover> CreateAsync(Cover cover)
        {
            ReceivedCover = cover;

            if (CreateException is not null)
            {
                return Task.FromException<Cover>(CreateException);
            }

            return Task.FromResult(CreatedCover);
        }

        public Task<bool> DeleteAsync(string id)
        {
            DeletedId = id;

            return Task.FromResult(id == _existingCover.Id);
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
}