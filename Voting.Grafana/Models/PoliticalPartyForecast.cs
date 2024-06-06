using System.ComponentModel.DataAnnotations;

namespace Voting.Grafana.Models;

/// <summary>
/// A class representing the forecast for a political party.
/// </summary>
public sealed class PoliticalPartyForecast
{
    #region Properties

    public string PartyCode { get; set; } = string.Empty;
    [Range(0.00, 100.00)]
    public double ForecastPercentage { get; set; } = 0;

    #endregion Properties
}
