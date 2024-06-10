using System.ComponentModel.DataAnnotations;

namespace Voting.Grafana.Models;

/// <summary>
/// A class representing a voting round.
/// </summary>
public sealed class VotingRound
{
    #region Properties

    public int Id { get; set; }
    public DateTimeOffset StartedTimestamp { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? EndedTimestamp { get; set; } = null;
    [Range(2020, 2100)]
    public uint VotingYear { get; set; }
    public bool IsRoundOpen { get; set; }
    public List<VoteCategory> ApplicableCategories { get; set; } = new();
    public uint ExpectedNumberOfVoters { get; set; }

    #endregion Properties

}

/// <summary>
/// A web model for creating a new voting round.
/// </summary>
public sealed class NewVotingRoundWebModel
{
    #region Properties

    [Range(2020, 2100)]
    public uint VotingYear { get; set; }
    public List<VoteCategory> ApplicableCategories { get; set; } = new();
    public uint ExpectedNumberOfVoters { get; set; }

    #endregion Properties
}

/// <summary>
/// A web model for the current status of the voting round.
/// </summary>
public sealed class VotingRoundCurrentStatusWebModel
{
    public bool VotingEnabled { get; set; }
    public string StatusString { get; set; } = string.Empty;
}

/// <summary>
/// A web model for the statistics of the voting round.
/// </summary>
public sealed class VotingRoundStatisticsWebModel
{
    public DateTimeOffset TimestampOfData { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset StartedTimestamp { get; set; }
    public DateTimeOffset? EndedTimestamp { get; set; } = null;
    public uint VotingYear { get; set; }
    public bool IsRoundOpen { get; set; }
    public List<string> ApplicableVotingCategories { get; set; } = new();
    public uint ExpectedNumberOfVoters { get; set; }
    public uint NumberOfVotes { get; set; }

}
