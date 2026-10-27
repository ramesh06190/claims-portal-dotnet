using ClaimsPortal.Core.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsPortal.Web.Api;

public sealed record ClaimDto(
    string ClaimNumber,
    string ClaimantName,
    BenefitProgram Program,
    DateOnly WeekEnding,
    bool WorkedThisWeek,
    decimal? GrossEarnings,
    bool AbleAndAvailable,
    decimal WeeklyAmount,
    ClaimStatus Status,
    DateTimeOffset SubmittedAt)
{
    // Claimant ID, email and phone are left out on purpose: the list endpoint never returns
    // more personal data than a caller needs.
    public static ClaimDto From(Claim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return new(claim.ClaimNumber, claim.ClaimantName, claim.Program, claim.WeekEnding, claim.WorkedThisWeek,
            claim.GrossEarnings, claim.AbleAndAvailable, claim.WeeklyAmount, claim.Status, claim.SubmittedAt);
    }
}

[ApiController]
[Route("api/claims")]
public sealed class ClaimsApiController(ClaimService claims) : ControllerBase
{
    /// <summary>Lists claims with paging and sorting.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ClaimDto>>(StatusCodes.Status200OK)]
    public async Task<PagedResult<ClaimDto>> List([FromQuery] ClaimQuery query, CancellationToken cancellationToken)
    {
        var page = await claims.QueryAsync(query, cancellationToken);
        return new PagedResult<ClaimDto>([.. page.Items.Select(ClaimDto.From)], page.Page, page.PageSize, page.TotalCount);
    }

    /// <summary>Gets one claim by its claim number, for example CL-024051.</summary>
    [HttpGet("{claimNumber}")]
    [ProducesResponseType<ClaimDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimDto>> Get(string claimNumber, CancellationToken cancellationToken)
    {
        var claim = await claims.GetAsync(claimNumber, cancellationToken);
        return claim is null ? Problem(statusCode: 404, title: $"Claim {claimNumber} was not found") : ClaimDto.From(claim);
    }

    /// <summary>Files a weekly claim. Validation errors come back as a 400 ValidationProblemDetails.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<ClaimDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClaimDto>> Submit(SubmitClaimRequest request, CancellationToken cancellationToken)
    {
        var result = await claims.SubmitAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(JsonName(error.Field), error.Message);
            }
            return ValidationProblem(ModelState);
        }

        var dto = ClaimDto.From(result.Claim!);
        return CreatedAtAction(nameof(Get), new { claimNumber = dto.ClaimNumber }, dto);
    }

    // Match the camelCase property names the client sent
    private static string JsonName(string field) => char.ToLowerInvariant(field[0]) + field[1..];
}
