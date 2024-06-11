namespace Voting.Grafana.Models;

/// <summary>
/// A class representing the basic informaion for a political party.
/// </summary>
public sealed class PoliticalParty
{
    #region Properties
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    #endregion Properties
}
