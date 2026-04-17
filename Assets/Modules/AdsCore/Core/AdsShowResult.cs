namespace Modules.AdsCore
{
    public enum AdsShowResultStatus
    {
        Completed = 0,
        Cancelled = 1,
        Failed = 2,
        Rejected = 3
    }

    public readonly struct AdsShowResult
    {
        public AdsShowResult(AdsShowResultStatus status, string message = null)
        {
            Status = status;
            Message = message ?? string.Empty;
        }

        public AdsShowResultStatus Status { get; }
        public string Message { get; }
        public bool IsSuccess => Status == AdsShowResultStatus.Completed;

        public static AdsShowResult Completed(string message = null) => new(AdsShowResultStatus.Completed, message);
        public static AdsShowResult Cancelled(string message = null) => new(AdsShowResultStatus.Cancelled, message);
        public static AdsShowResult Failed(string message = null) => new(AdsShowResultStatus.Failed, message);
        public static AdsShowResult Rejected(string message = null) => new(AdsShowResultStatus.Rejected, message);
    }
}
