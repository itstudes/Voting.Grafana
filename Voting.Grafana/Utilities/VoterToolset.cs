namespace Voting.Grafana.Utilities;

/// <summary>
/// A class containing tools for vote data and voting functionality
/// </summary>
public static class VoterToolset
{
    /// <summary>
    /// Generates a typical South African voter id number.
    /// </summary>
    /// <example>9202204720082</example>
    /// <returns></returns>
    public static string GenerateVoterIdNumber()
    {
        var random = new Random();
        //get first 6 digits of the id number -date of birth (YYMMDD)
        var year = random.Next(0, 100);
        var month = random.Next(1, 12);
        var day = random.Next(1, 28);
        //get next 4 digits of the id number -gender (SSSS)
        var sequence = random.Next(0, 9999);
        //get the last digit of the id number -SA citizen / foreigner (C)
        var control = random.Next(0, 1);
        //get the second last digit (A)
        var secondLastDigit = random.Next(0, 7);
        //do the final checksum
        var checkSum = (year ^ month ^ day ^ sequence ^ control ^ secondLastDigit) % 10;

        return $"{year:D2}{month:D2}{day:D2}{sequence:D4}{control}{secondLastDigit}{checkSum}";
    }

    /// <summary>
    /// Checks if the voter id number is valid.
    /// </summary>
    /// <param name="voterId"></param>
    /// <returns></returns>
    public static bool CheckIfIdIsValid(string voterId)
    {
        //check if the id number is 13 digits long
        if ( voterId.Length != 13 )
        {
            return false;
        }
        //check if the id only contains numbers
        if ( !voterId.All(char.IsDigit) )
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Gets the final vote submission from the voter's vote request. Also gets a time to vote based on the number of parties the voter is considering.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="votingRoundId"></param>
    /// <param name="finalSubmissions"></param>
    /// <param name="finalTimeToVote_ms"></param>
    /// <param name="forecastsForApplicableParties"></param>
    public static void GetVoteSubmissionsFromVoteRequest(VoteRequest request,
                                                         int votingRoundId,
                                                         out List<VoteSubmission> finalSubmissions,
                                                         out int finalTimeToVote_ms,
                                                         List<PoliticalPartyForecast>? forecastsForApplicableParties = null)
    {
        //default data
        finalSubmissions = new List<VoteSubmission>();
        finalTimeToVote_ms = 0;
        var random = new Random();
        var randomScalingFactor = random.NextDouble() * 0.1;

        //iterate through the vote intentions for each category and create a vote submission for each
        foreach ( var intentionsForCategories in request.VotingIntentions )
        {
            //iterate through all the parties the voter is considering and choose one
            // - base the choice on the forecast if available (if the voter is undecided)
            // - base the choice on the voter's intention too
            double selectedPartyConfidencePercentage = 0;
            string selectedPartyCode = string.Empty;
            foreach ( var intention in intentionsForCategories.Intentions )
            {
                //get the forecast for the party
                var forecast = forecastsForApplicableParties?.FirstOrDefault(f => f.PartyCode == intention.PartyCode);
                

                //if there is a forecast, use the multiple of that and the voter's intention to get the confidence percentage
                var currentPartyConfidencePercentage = randomScalingFactor * intention.IntentionPercentage;
                if ( forecast != null )
                {
                    currentPartyConfidencePercentage = randomScalingFactor * (forecast.ForecastPercentage / 100) * intention.IntentionPercentage;
                }

                //if the current party has a higher confidence percentage, select it
                if ( currentPartyConfidencePercentage > selectedPartyConfidencePercentage )
                {
                    selectedPartyConfidencePercentage = currentPartyConfidencePercentage;
                    selectedPartyCode = intention.PartyCode;
                }
            }

            //create the vote submission
            finalSubmissions.Add(new VoteSubmission
            {
                VotingRoundId = votingRoundId,
                VoterIdNumber = request.VoterIdNumber,
                PoliticalPartyCode = selectedPartyCode,
                Category = intentionsForCategories.Category
            });           
        }

        //add the time to vote
        // - the time to vote is the time it takes for the user to vote for the party
        // - the time to vote is in milliseconds
        // - the time to vote is a random number between 1 and 5 seconds
        // - the time to vote is scaled by the number of parties the voter is considering
        finalTimeToVote_ms = (int)(Math.Round(randomScalingFactor * random.Next(1000, 5000) * request.GetPartyCodesVoterIsConsidering().Count));
    }
}
