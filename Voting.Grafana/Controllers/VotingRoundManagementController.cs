using Microsoft.AspNetCore.Mvc;

namespace Voting.Grafana.Controllers;

[ApiController]
[Route("manage")]
public class VotingRoundManagementController : ControllerBase
{
    private readonly ILogger<VotingRoundManagementController> _logger;
    private readonly VotingRoundManagementService _votingRoundManagementService;

    public VotingRoundManagementController(ILogger<VotingRoundManagementController> logger,
                                           VotingRoundManagementService votingRoundManagementService)
    {
        _logger = logger;
        _votingRoundManagementService = votingRoundManagementService;
    }

    [HttpGet("status")]
    public IActionResult GetVotingRoundStatus()
    {
        VotingRoundCurrentStatusWebModel votingRoundStatus = null;
        if ( _votingRoundManagementService.CanVote(out var results) )
        {
            votingRoundStatus = new VotingRoundCurrentStatusWebModel
            {
                Status = "Voting is open"
            };
        }
        else
        {
            votingRoundStatus = new VotingRoundCurrentStatusWebModel
            {
                Status = results
            };
        }
        return Ok(votingRoundStatus);
    }

    [HttpGet("statistics")]
    public IActionResult GetVotingRoundStatistics()
    {
        try
        {
            if ( _votingRoundManagementService.CanVote(out var results) )
            {
                //get statistics
                var currentVotingRound = _votingRoundManagementService.CurrentVotingRound;
                var numberOfVotes = _votingRoundManagementService.CurrentNumberOfVotes;
                var statisticsData = new VotingRoundStatisticsWebModel
                {
                    VotingYear = currentVotingRound.VotingYear,
                    StartedTimestamp = currentVotingRound.StartedTimestamp,
                    EndedTimestamp = currentVotingRound.EndedTimestamp,
                    IsRoundOpen = currentVotingRound.IsRoundOpen,
                    ApplicableVotingCategories = currentVotingRound.ApplicableCategories.Select(vc => GetStringFromVoteCategory(vc)).ToList(),
                    ExpectedNumberOfVoters = currentVotingRound.ExpectedNumberOfVoters,
                    NumberOfVotes = numberOfVotes
                };
                return Ok(statisticsData);
            }
            else
            {
                var votingRoundStatus = new VotingRoundCurrentStatusWebModel
                {
                    Status = results
                };
                //return HTTP 405 error
                return StatusCode(StatusCodes.Status405MethodNotAllowed, votingRoundStatus);
            }
        }
        catch ( Exception ex )
        {
            var exceptionModel = new InternalExceptionWebModel
            {
                Code = ex.HResult.ToString(),
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            };
            //return HTTP 500 error
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionModel);
        }
    }

    [HttpGet("archive")]
    public IActionResult GetVotingRoundArchive()
    {
        try
        {
            var archiveData = _votingRoundManagementService.GetArchiveData();
            return Ok(archiveData);
        }
        catch ( Exception ex )
        {
            var exceptionModel = new InternalExceptionWebModel
            {
                Code = ex.HResult.ToString(),
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            };
            //return HTTP 500 error
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionModel);
        }
    }

    [HttpPost("new-round")]
    public IActionResult CreateNewVotingRound([FromBody] NewVotingRoundWebModel newVotingRound)
    {
        try
        {
            var newRoundData = _votingRoundManagementService.StartVotingRound(newVotingRound.VotingYear,
                                                                              newVotingRound.ApplicableCategories,
                                                                              newVotingRound.ExpectedNumberOfVoters);
            return Ok(newRoundData);
        }
        catch ( Exception ex )
        {
            var exceptionModel = new InternalExceptionWebModel
            {
                Code = ex.HResult.ToString(),
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            };
            //return HTTP 500 error
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionModel);
        }
    }

    [HttpGet("end-round")]
    public IActionResult EndVotingRound()
    {
        try
        {
            var returnData = _votingRoundManagementService.EndCurrentVotingRound();
            return Ok(returnData);
        }
        catch ( Exception ex )
        {
            var exceptionModel = new InternalExceptionWebModel
            {
                Code = ex.HResult.ToString(),
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            };
            //return HTTP 500 error
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionModel);
        }
    }
}
