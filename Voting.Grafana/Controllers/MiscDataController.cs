using Microsoft.AspNetCore.Mvc;

namespace Voting.Grafana.Controllers;

[ApiController]
[Route("misc")]
public class MiscDataController : ControllerBase
{
    public MiscDataController(ILogger<MiscDataController> logger)
    {
    }

    [HttpGet("vote-categories")]
    public IActionResult GetVoteCategories()
    {
        Dictionary<int, string> voteCategories = new();
        foreach (VoteCategory voteCategory in Enum.GetValues(typeof(VoteCategory)))
        {
            voteCategories.Add((int)voteCategory, GetStringFromVoteCategory(voteCategory));
        }
        return Ok(voteCategories);
    }

    [HttpGet("generate-id")]
    public IActionResult GenerateIdNumber()
    {
        var generatedIdNumber = VoterToolset.GenerateVoterIdNumber();
        return Ok(generatedIdNumber);
    }
}
