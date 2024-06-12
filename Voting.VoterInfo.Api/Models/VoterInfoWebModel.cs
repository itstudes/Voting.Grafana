namespace Voting.VoterInfo.Api.Models;

/// <summary>
/// Stores information about a voter
/// </summary>
public class VoterInfoWebModel
{
    public string IdNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
