using Claims.Exceptions;
using Claims.Models;
using Claims.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

[ApiController]
[Route("[controller]")]
public class ClaimsController : ControllerBase
{
    private readonly IClaimService _claimService;

    public ClaimsController(IClaimService claimService)
    {
        _claimService = claimService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Claim>>> GetAsync()
    {
        var claims = await _claimService.GetAllAsync();

        return Ok(claims);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Claim>> GetAsync(string id)
    {
        var claim = await _claimService.GetByIdAsync(id);

        if (claim is null)
        {
            return NotFound();
        }

        return Ok(claim);
    }

    [HttpPost]
    public async Task<ActionResult<Claim>> CreateAsync(Claim claim)
    {
        try
        {
            var createdClaim = await _claimService.CreateAsync(claim);

            return Ok(createdClaim);
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
        var deleted = await _claimService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}