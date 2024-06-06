namespace Voting.Grafana.Models;

/// <summary>
/// A class representing a person's thoughts on the political parties when they go into vote. 
/// This is also the webmodel for the vote submission.
/// </summary>
public sealed class VoteRequest
{
    #region Properties

    public string VoterIdNumber { get; set; } = string.Empty;
    public List<VoteIntentionsForVotingCategory> VotingIntentions { get; set; } = [];

    #endregion Properties

    #region Public Functions

    public List<string> GetPartyCodesVoterIsConsidering()
    {
        List<string> partyCodes = new List<string>();
        foreach ( var intentions in VotingIntentions.Select(vifvc => vifvc.Intentions) )
        {
            foreach ( var intention in intentions )
            {
                partyCodes.Add(intention.PartyCode);
            }
        }
        return [.. partyCodes.Distinct().Order()];
    }

    #endregion Public Functions
}
