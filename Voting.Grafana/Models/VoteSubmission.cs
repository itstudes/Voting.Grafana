namespace Voting.Grafana.Models;

/// <summary>
/// A class representing a final vote submission.
/// </summary>
public sealed class VoteSubmission
{
    #region Properties

    public int VotingRoundId { get; set; }
    public string VoterIdNumber { get; set; } = string.Empty;
    public string PoliticalPartyCode { get; set; } = string.Empty;
    public VoteCategory Category { get; set; } = VoteCategory.National;

    #endregion Properties
}

/// <summary>
/// A web model to represent a vote the final vote submission for a voter.
/// </summary>
public sealed class VoteSubmissionWebModel
{
    #region Properties
    public uint VotingYear { get; set; }
    public string VoterIdNumber { get; set; } = string.Empty;
    public int TimeToVote_ms { get; set; }
    public List<VoteSubmissionForCategoryWebModel> VoteSubmissions { get; set; } = [];

    #endregion Properties

    /// <summary>
    /// A support class for the VoteSubmissionWebModel.
    /// </summary>
    public sealed class VoteSubmissionForCategoryWebModel
    {
        public string PoliticalPartyVoted { get; set; } = string.Empty;
        public VoteCategory Category { get; set; } = VoteCategory.National;
    }
}