namespace Voting.Grafana.Services;

/// <summary>
/// A singleton service that provides the list of registered political parties and their performance forecasts.
/// </summary>
public sealed class RegisteredPartiesService
{
    private List<PoliticalParty> _parties = new();
    private List<PoliticalPartyForecast> _partyPerformanceForecasts = new();

    #region Properties

    public IEnumerable<PoliticalParty> RegisteredParties => _parties;
    public IEnumerable<PoliticalPartyForecast> PartyPerformanceForecasts => _partyPerformanceForecasts;

    #endregion Properties

    #region Constructors

    public RegisteredPartiesService()
    {
        SeedPoliticalPartiesAndPerformance();
    }

    #endregion Constructors

    #region Public Functions

    public bool IsPartyRegistered(string partyCode) =>
        _parties.Any(pp => pp.Code == partyCode.Trim().ToUpper());

    #endregion Public Functions

    #region Private Functions

    private void SeedPoliticalPartiesAndPerformance()
    {
        //seed 20 parties
        _parties =
            [
                new() { Code = "ANC", Name = "African National Congress" },
                new() { Code = "DA", Name = "Democratic Alliance" },
                new() { Code = "MK", Name = "Umkhonto we Sizwe" },
                new() { Code = "EFF", Name = "Economic Freedom Fighters" },
                new() { Code = "IFP", Name = "Inkatha Freedom Party" },

                new() { Code = "PA", Name = "Patriotic Alliance" },
                new() { Code = "VF+", Name = "Freedom Front Plus" },
                new() { Code = "ACTIONSA", Name = "ActionSA" },
                new() { Code = "RISE", Name = "Rise Mzansi" },
                new() { Code = "UDM", Name = "United Democratic Movement" },

                new() { Code = "BOSA", Name = "Build One South Africa" },
                new() { Code = "ATM", Name = "African Transformation Movement" },
                new() { Code = "GOOD", Name = "Good" },
                new() { Code = "COPE", Name = "Congress of the People" },
                new() { Code = "ACDP", Name = "African Christian Democratic Party" },

                new() { Code = "PAC", Name = "Pan Africanist Congress of Azania" },
                new() { Code = "UAT", Name = "United Africans Transformation" },
                new() { Code = "NFP", Name = "National Freedom Party" },
                new() { Code = "ALJAMA", Name = "Al Jama-ah" },
                new() { Code = "CCC", Name = "National Coloured Congress"}
            ];

        //seed performance forecast for 20 parties
        _partyPerformanceForecasts =
            [
                new() {PartyCode = "ANC", ForecastPercentage = 40},
                new() {PartyCode = "DA", ForecastPercentage = 20},
                new() {PartyCode = "MK", ForecastPercentage = 15},
                new() {PartyCode = "EFF", ForecastPercentage = 10},
                new() {PartyCode = "IFP", ForecastPercentage = 3},

                new() {PartyCode = "PA", ForecastPercentage = 2},
                new() {PartyCode = "VF+", ForecastPercentage = 1.5},
                new() {PartyCode = "ACTIONSA", ForecastPercentage = 1.5},
                new() {PartyCode = "RISE", ForecastPercentage = 1},
                new() {PartyCode = "UDM", ForecastPercentage = 1},

                new() {PartyCode = "BOSA", ForecastPercentage = 0.5},
                new() {PartyCode = "ATM", ForecastPercentage = 0.5},
                new() {PartyCode = "GOOD", ForecastPercentage = 0.5},
                new() {PartyCode = "COPE", ForecastPercentage = 0.5},
                new() {PartyCode = "ACDP", ForecastPercentage = 0.5},

                new() {PartyCode = "PAC", ForecastPercentage = 0.5},
                new() {PartyCode = "UAT", ForecastPercentage = 0.5},
                new() {PartyCode = "NFP", ForecastPercentage = 0.5},
                new() {PartyCode = "ALJAMA", ForecastPercentage = 0.5},
                new() {PartyCode = "CCC", ForecastPercentage = 0.5}
            ];
    }


    #endregion Private Functions
}
