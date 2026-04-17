using System;
using System.Collections.Generic;

namespace Modules.PurchasesCore
{
    public interface IPurchaseProvider
    {
        string ProviderId { get; }
        bool IsInitialized { get; }
        bool IsPurchaseInProgress { get; }

        void Initialize();
        bool TryPurchase(string productId, Action<PurchaseResult> callback);
        IReadOnlyList<PurchaseProductInfo> GetProducts();
    }
}
