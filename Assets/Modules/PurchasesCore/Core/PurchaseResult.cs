namespace Modules.PurchasesCore
{
    public enum PurchaseResultStatus
    {
        Completed = 0,
        Failed = 1,
        Cancelled = 2,
        Rejected = 3
    }

    public readonly struct PurchaseResult
    {
        public PurchaseResult(PurchaseResultStatus status, string message = null)
        {
            Status = status;
            Message = message ?? string.Empty;
        }

        public PurchaseResultStatus Status { get; }
        public string Message { get; }
        public bool IsSuccess => Status == PurchaseResultStatus.Completed;

        public static PurchaseResult Completed(string message = null) => new(PurchaseResultStatus.Completed, message);
        public static PurchaseResult Failed(string message = null) => new(PurchaseResultStatus.Failed, message);
        public static PurchaseResult Cancelled(string message = null) => new(PurchaseResultStatus.Cancelled, message);
        public static PurchaseResult Rejected(string message = null) => new(PurchaseResultStatus.Rejected, message);
    }
}
