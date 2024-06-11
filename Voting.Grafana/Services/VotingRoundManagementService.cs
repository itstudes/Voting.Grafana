using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Voting.Grafana.Services;

/// <summary>
/// A singleton service that manages the voting rounds.
/// </summary>
public class VotingRoundManagementService
{
    //diagnostics
    private readonly ActivitySource _activitySource;
    private readonly Counter<long> _votesCounter;

    //other services
    private readonly RegisteredPartiesService _registeredPartiesService;

    //data
    private VotingRound? _currentVotingRound = null;
    private ConcurrentDictionary<string, VoteSubmission> _currentVotes = [];
    private Dictionary<VotingRound, List<VoteSubmission>> _archive = [];
    private object _votingRoundlock = new();

    #region Properties

    public VotingRound? CurrentVotingRound => _currentVotingRound;
    public uint CurrentNumberOfVotes => (uint)_currentVotes.Count;

    #endregion Properties

    #region Constructors

    public VotingRoundManagementService(RegisteredPartiesService registeredPartiesService,
                                        AppInstrumentation appInstrumentation)
    {
        _registeredPartiesService = registeredPartiesService;

        //metrics
        _activitySource = appInstrumentation.ActivitySource;
        _votesCounter = appInstrumentation.VotesTotalCounter;
    }

    #endregion Constructors

    #region Public Functions

    public List<VotingRoundStatisticsWebModel> GetArchiveData()
    {
        return _archive.Select(kvp => new VotingRoundStatisticsWebModel
        {
            VotingYear = kvp.Key.VotingYear,
            IsRoundOpen = kvp.Key.IsRoundOpen,
            StartedTimestamp = kvp.Key.StartedTimestamp,
            EndedTimestamp = kvp.Key.EndedTimestamp,
            ExpectedNumberOfVoters = kvp.Key.ExpectedNumberOfVoters,
            NumberOfVotes = (uint)kvp.Value.Count,
            ApplicableVotingCategories = kvp.Key.ApplicableCategories.Select(vc => GetStringFromVoteCategory(vc)).ToList(),
        })
                       .ToList();
    }

    public VotingRoundStatisticsWebModel EndCurrentVotingRound()
    {
        //checks
        if (_currentVotingRound is null)
        {
            throw new Exception("No voting round is currently in progress.");
        }

        //set the current voting round as closed and set the return data
        _currentVotingRound.IsRoundOpen = false;
        _currentVotingRound.EndedTimestamp = DateTimeOffset.Now;
        var returnData = new VotingRoundStatisticsWebModel
        {
            VotingYear = _currentVotingRound.VotingYear,
            IsRoundOpen = _currentVotingRound.IsRoundOpen,
            StartedTimestamp = _currentVotingRound.StartedTimestamp,
            EndedTimestamp = _currentVotingRound.EndedTimestamp,
            ExpectedNumberOfVoters = _currentVotingRound.ExpectedNumberOfVoters,
            NumberOfVotes = (uint)_currentVotes.Count,
            ApplicableVotingCategories = _currentVotingRound.ApplicableCategories.Select(vc => GetStringFromVoteCategory(vc)).ToList(),
        };

        //archive the current voting round and prepare for a new round
        lock (_votingRoundlock)
        {
            _archive.Add(_currentVotingRound, _currentVotes.Values?.ToList() ?? new());
            _currentVotingRound = null;
            _currentVotes.Clear();
        }

        return returnData;
    }

    public VotingRound StartVotingRound(uint year,
                                        List<VoteCategory> applicableCategories,
                                        uint expectedNumberOfVoters)
    {
        //checks
        //check if a voting round is already in progress
        if (_currentVotingRound is not null)
        {
            Log.Warning("A voting round is already in progress (votingRoundId = {votingRoundId}). End that voting round first before trying to start a new one.", _currentVotingRound.Id);
            return _currentVotingRound;
        }
        //check if the year is valid
        if (year < 2020 || year > 2100)
        {
            throw new Exception("The year must be between 2020 and 2100.");
        }
        //check if the year is greater than the previous round's years
        if (_archive.Keys.Any() && year <= _archive.Keys.Select(vr => vr.VotingYear).Max())
        {
            throw new Exception("The year must be greater than the previous round's year.");
        }
        //check if the number of voters is valid
        if (expectedNumberOfVoters < 1)
        {
            throw new Exception("The number of voters must be at least 1.");
        }
        //check that the categories are valid
        if (applicableCategories.Count == 0)
        {
            throw new Exception("At least one category must be provided.");
        }

        //create a new voting round
        lock (_votingRoundlock)
        {
            _currentVotingRound = new VotingRound
            {
                Id = _archive.Keys.Count + 1,
                VotingYear = year,
                IsRoundOpen = true,
                ApplicableCategories = applicableCategories.Distinct().ToList(),
                ExpectedNumberOfVoters = expectedNumberOfVoters
            };
        }

        return _currentVotingRound;
    }

    public bool CanVote(out string results)
    {
        results = string.Empty;

        //checks
        //check if a voting round is in progress
        if (_currentVotingRound is null)
        {
            results = "No voting round is currently in progress.";
            return false;
        }
        if (!_currentVotingRound.IsRoundOpen)
        {
            results = "The current voting round is closed.";
            return false;
        }

        return true;
    }

    public bool IsVoteValidForRound(VoteSubmission vote,
                                    out string results)
    {
        results = string.Empty;

        //checks
        if (!CanVote(out results))
        {
            return false;
        }
        //check if the vote is valid for the current round
        //check category
        if (!_currentVotingRound.ApplicableCategories.Contains(vote.Category))
        {
            results = $"The submitted vote category (value = {vote.Category}) is not applicable to the current voting round.";
            return false;
        }
        //check if the voter has already voted
        if (_currentVotes.ContainsKey($"{vote.VoterIdNumber}[{GetStringFromVoteCategory(vote.Category).ToUpper()}]"))
        {
            results = $"The voter has already voted in this round (Voter ID = {vote.VoterIdNumber}).";
            return false;
        }
        //check if the voter is voting for a valid party
        if (!_registeredPartiesService.IsPartyRegistered(vote.PoliticalPartyCode))
        {
            results = $"The voter has voted for a political party that is not registered (value = {vote.PoliticalPartyCode}).";
            return false;
        }

        return true;
    }

    public void SubmitVote(VoteSubmission vote)
    {
        _currentVotes.AddOrUpdate(key: $"{vote.VoterIdNumber}[{GetStringFromVoteCategory(vote.Category).ToUpper()}]",
                                  addValue: vote,
                                  updateValueFactory: (key, oldValue) => vote);

        //add to the counter, for metrics
        //tag with: 
        //- VotingRoundId,
        //- PoliticalPartyCode,
        //- Category
        _votesCounter.Add(delta: 1,
                          tag1: new KeyValuePair<string, object?>("VotingRoundId", vote.VotingRoundId),
                          tag2: new KeyValuePair<string, object?>("PoliticalPartyCode", vote.PoliticalPartyCode),
                          tag3: new KeyValuePair<string, object?>("Category", GetStringFromVoteCategory(vote.Category)));
    }


    #endregion Public Functions

}

