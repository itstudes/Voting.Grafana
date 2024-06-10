using Microsoft.AspNetCore.Mvc;

namespace Voting.Grafana.Controllers;

[ApiController]
[Route("vote")]
public class VotingController : ControllerBase
{
    private readonly AppInstrumentation _appInstrumentation;
    private readonly VotingRoundManagementService _votingRoundManagementService;
    private readonly RegisteredPartiesService _registeredPartiesService;

    public VotingController(AppInstrumentation appInstrumentation,
                            VotingRoundManagementService votingRoundManagementService,
                            RegisteredPartiesService registeredPartiesService)
    {
        _appInstrumentation = appInstrumentation;
        _votingRoundManagementService = votingRoundManagementService;
        _registeredPartiesService = registeredPartiesService;
    }

    [HttpPost]
    public IActionResult Vote(VoteRequest voteRequest)
    {
        using var _ = _appInstrumentation.MeasureVoteDuration_ms();
        try
        {
            //checks
            //valid ID
            if ( string.IsNullOrEmpty(voteRequest.VoterIdNumber) )
            {
                return BadRequest("Voter ID number is required.");
            }
            //voting data
            if ( voteRequest.VotingIntentions is null || (voteRequest.VotingIntentions is not null && voteRequest.VotingIntentions.Count == 0) )
            {
                return BadRequest("Voting intentions are required to make a vote.");
            }
            //check if voting is open
            if ( !_votingRoundManagementService.CanVote(out var canVoteResults) )
            {
                return BadRequest(canVoteResults);
            }

            //get vote submissions for request
            var partiesConsideredByVoter = voteRequest.GetPartyCodesVoterIsConsidering();
            var partyForecasts = _registeredPartiesService.PartyPerformanceForecasts.Where(ppf => partiesConsideredByVoter.Contains(ppf.PartyCode))
                                                                                    .ToList();
            VoterToolset.GetVoteSubmissionsFromVoteRequest(voteRequest,
                                                           _votingRoundManagementService.CurrentVotingRound.Id,
                                                           out var submissions,
                                                           out var finalTimeToVote_ms,
                                                           partyForecasts);

            //final checks on if the vote is valid / can be submitted
            foreach ( var submission in submissions )
            {
                if ( !_votingRoundManagementService.IsVoteValidForRound(submission, out var areSubmissionsValidResults) )
                {
                    return BadRequest(areSubmissionsValidResults);
                }
            }

            //wait the designated time to vote
            Thread.Sleep(finalTimeToVote_ms);

            //submit the votes
            foreach ( var submission in submissions )
            {
                _votingRoundManagementService.SubmitVote(submission);
            }

            //get the vote return data
            var voteReturnData = new VoteSubmissionWebModel
            {
                VoterIdNumber = voteRequest.VoterIdNumber,
                VotingYear = _votingRoundManagementService.CurrentVotingRound.VotingYear,
                TimeToVote_ms = finalTimeToVote_ms,
                VoteSubmissions = submissions.Select(s => new VoteSubmissionWebModel.VoteSubmissionForCategoryWebModel
                {
                    PoliticalPartyVoted = s.PoliticalPartyCode,
                    Category = s.Category
                }).ToList()
            };

            return Ok(voteReturnData);
        }
        catch ( Exception ex )
        {
            //log error
            Log.Error(ex, "An error occurred while trying to vote.");

            //return HTTP 500 error
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
