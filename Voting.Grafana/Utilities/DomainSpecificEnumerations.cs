namespace Voting.Grafana.Utilities;

/// <summary>
/// All the domain specific enumerations are defined here.
/// </summary>
public static class DomainSpecificEnumerations
{
    #region vote category

    /// <summary>
    /// This enumeration represents the category of the vote.
    /// </summary>
    public enum VoteCategory
    {
        National,
        Provincial,
        Local
    }

    /// <summary>
    /// Gets the string representation of the vote category.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public static string GetStringFromVoteCategory(VoteCategory category)
    {
        return category switch
        {
            VoteCategory.National => "National",
            VoteCategory.Provincial => "Provincial",
            VoteCategory.Local => "Local",
            _ => "National"
        };
    }

    #endregion vote category
}
