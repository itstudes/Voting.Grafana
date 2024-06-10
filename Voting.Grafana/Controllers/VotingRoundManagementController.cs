using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Voting.Grafana.Controllers;

[ApiController]
[Route("manage")]
public class VotingRoundManagementController : ControllerBase
{
    private readonly VotingRoundManagementService _votingRoundManagementService;
    private readonly ActivitySource _activitySource;

    public VotingRoundManagementController(AppInstrumentation appInstrumentation,
                                           VotingRoundManagementService votingRoundManagementService)
    {
        _activitySource = appInstrumentation.ActivitySource;
        _votingRoundManagementService = votingRoundManagementService;
    }

    [HttpGet("status")]
    public IActionResult GetVotingRoundStatus()
    {
        VotingRoundCurrentStatusWebModel? votingRoundStatus = null;
        if ( _votingRoundManagementService.CanVote(out var results) )
        {
            votingRoundStatus = new VotingRoundCurrentStatusWebModel
            {
                VotingEnabled = true,
                StatusString = "Voting is open"
            };
        }
        else
        {
            votingRoundStatus = new VotingRoundCurrentStatusWebModel
            {
                VotingEnabled = false,
                StatusString = results
            };
        }
        return Ok(votingRoundStatus);
    }

    [HttpGet("statistics")]
    public IActionResult GetVotingRoundStatistics()
    {
        using var activity = _activitySource.StartActivity("VotingRoundManagementController.GetVotingRoundStatistics");
        try
        {
            if (_votingRoundManagementService.CanVote(out var results))
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

                //set success status
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Ok(statisticsData);
            }
            else
            {
                var votingRoundStatus = new VotingRoundCurrentStatusWebModel
                {
                    StatusString = results
                };
                //return HTTP 405 error
                return StatusCode(StatusCodes.Status405MethodNotAllowed, votingRoundStatus);
            }
        }
        catch (Exception ex)
        {
            //log the exception
            Log.Error(ex, "An exception occurred while getting voting round statistics.");

            //set failure activity status
            activity?.SetStatus(ActivityStatusCode.Error);
         
            //return HTTP 500 error
            var exceptionModel = new InternalExceptionWebModel
            {
                Code = ex.HResult.ToString(),
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            };
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionModel);
        }
    }

    [HttpGet("archive")]
    public IActionResult GetVotingRoundArchive()
    {
        using var activity = _activitySource.StartActivity("VotingRoundManagementController.GetVotingRoundArchive");
        try
        {
            var archiveData = _votingRoundManagementService.GetArchiveData();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return Ok(archiveData);
        }
        catch ( Exception ex )
        {
            //log the exception
            Log.Error(ex, "An exception occurred while getting voting round archive data.");

            //set failure activity status
            activity?.SetStatus(ActivityStatusCode.Error);
         
            //return HTTP 500 error
            var exceptionModel = new InternalExceptionWebModel
            {
                Code = ex.HResult.ToString(),
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            };            
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionModel);
        }
    }

    [HttpPost("new-round")]
    public IActionResult CreateNewVotingRound([FromBody] NewVotingRoundWebModel newVotingRound)
    {
        using var activity = _activitySource.StartActivity("VotingRoundManagementController.CreateNewVotingRound");
        try
        {
            var newRoundData = _votingRoundManagementService.StartVotingRound(newVotingRound.VotingYear,
                                                                              newVotingRound.ApplicableCategories,
                                                                              newVotingRound.ExpectedNumberOfVoters);
            return Ok(newRoundData);
        }
        catch ( Exception ex )
        {
            //log the exception
            Log.Error(ex, "An exception occurred while creating a new voting round.");

            //set failure activity status
            activity?.SetStatus(ActivityStatusCode.Error);

            //return HTTP 500 error
            var exceptionModel = new InternalExceptionWebModel
            {
                Code = ex.HResult.ToString(),
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            };            
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionModel);
        }
    }

    [HttpGet("end-round")]
    public IActionResult EndVotingRound()
    {
        using var activity = _activitySource.StartActivity("VotingRoundManagementController.EndVotingRound");
        try
        {
            var returnData = _votingRoundManagementService.EndCurrentVotingRound();
            return Ok(returnData);
        }
        catch ( Exception ex )
        {
            //log the exception
            Log.Error(ex, "An exception occurred while ending the current voting round.");

            //set failure activity status
            activity?.SetStatus(ActivityStatusCode.Error);

            //return HTTP 500 error
            var exceptionModel = new InternalExceptionWebModel
            {
                Code = ex.HResult.ToString(),
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            };
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionModel);
        }
    }
}
