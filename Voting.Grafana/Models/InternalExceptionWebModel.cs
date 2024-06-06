namespace Voting.Grafana.Models
{
    /// <summary>
    /// A web model class representing an internal exception on the server.
    /// </summary>
    public sealed class InternalExceptionWebModel
    {
        #region Properties

        public string ExceptionType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        #endregion Properties
    }
}
