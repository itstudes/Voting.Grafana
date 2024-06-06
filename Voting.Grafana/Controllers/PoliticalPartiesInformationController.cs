using Microsoft.AspNetCore.Mvc;

namespace Voting.Grafana.Controllers;

[ApiController]
[Route("parties")]
public class PoliticalPartiesInformationController : ControllerBase
{
    private readonly ILogger<PoliticalPartiesInformationController> _logger;
    private readonly RegisteredPartiesService _registeredPartiesService;

    public PoliticalPartiesInformationController(ILogger<PoliticalPartiesInformationController> logger, 
                                                 RegisteredPartiesService registeredPartiesService)
    {
        _logger = logger;
        _registeredPartiesService = registeredPartiesService;
    }

    [HttpGet("registered")]
    public IActionResult GetParties()
    {
        var parties = _registeredPartiesService.RegisteredParties.OrderBy(pp => pp.Code);
        return Ok(parties);
    }

    [HttpGet("forecasts")]
    public IActionResult GetForecasts()
    {
        var forecasts = _registeredPartiesService.PartyPerformanceForecasts.OrderBy(ppf => ppf.PartyCode);
        return Ok(forecasts);
    }
}
