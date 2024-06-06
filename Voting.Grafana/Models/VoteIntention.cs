using System.ComponentModel.DataAnnotations;

namespace Voting.Grafana.Models;

/// <summary>
/// A class representing a person's thoughts/leaning on the political parties when they go into vote.
/// </summary>
public sealed class VoteIntention
{
    public string PartyCode { get; set; } = string.Empty;
    [Range(0.00, 100.00)]
    public double IntentionPercentage { get; set; } = 0;
}

/// <summary>
/// A class representing a person's thoughts on the political parties when they go into vote for a specific category.
/// </summary>
public sealed class VoteIntentionsForVotingCategory
{
    public VoteCategory Category { get; set; } = VoteCategory.National;
    public List<VoteIntention> Intentions { get; set; } = [];
}
