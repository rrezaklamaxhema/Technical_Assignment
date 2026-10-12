using Claims.Exceptions;
using Claims.Models;
using Claims.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

[ApiController]
[Route("[controller]")]
public class CoversController : ControllerBase
{
    private readonly ICoverService _coverService;
    private readonly IPremiumCalculator _premiumCalculator;

    public CoversController(
        ICoverService coverService,
        IPremiumCalculator premiumCalculator)
    {
        _coverService = coverService;
        _premiumCalculator = premiumCalculator;
    }

    [HttpPost("compute")]
    public ActionResult<decimal> ComputePremium(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] CoverType coverType)
    {
        var premium = _premiumCalculator.ComputePremium(
            startDate,
            endDate,
            coverType);

        return Ok(premium);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cover>>> GetAsync()
    {
        var covers = await _coverService.GetAllAsync();

        return Ok(covers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Cover>> GetAsync(string id)
    {
        var cover = await _coverService.GetByIdAsync(id);

        if (cover is null)
        {
            return NotFound();
        }

        return Ok(cover);
    }

    [HttpPost]
    public async Task<ActionResult<Cover>> CreateAsync(Cover cover)
    {
        try
        {
            var createdCover = await _coverService.CreateAsync(cover);

            return Ok(createdCover);
        }
        catch (ValidationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        var deleted = await _coverService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}