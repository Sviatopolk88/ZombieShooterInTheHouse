using System;
using System.Collections.Generic;
using YG;
using YG.Utils.Pay;

namespace Modules.PurchasesCore
{
    /// <summary>
    /// Reusable provider покупок поверх PluginYG2 / YG2.
    /// </summary>
    public sealed class YandexPurchaseProvider : IPurchaseProvider
    {
        private readonly List<PurchaseProductInfo> products = new();

        private Action<PurchaseResult> pendingCallback;
        private string pendingProductId = string.Empty;
        private bool initialized;
        private bool catalogLoaded;

        public string ProviderId => "yandex-games";
        public bool IsInitialized => YG2.isSDKEnabled && catalogLoaded;
        public bool IsPurchaseInProgress => pendingCallback != null;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

#if Payments_yg
            YG2.onGetPayments += OnGetPayments;
            YG2.onPurchaseSuccess += OnPurchaseSuccess;
            YG2.onPurchaseFailed += OnPurchaseFailed;
            RefreshProductsFromPlugin();
#endif

            initialized = true;
        }

        public bool TryPurchase(string productId, Action<PurchaseResult> callback)
        {
#if !Payments_yg
            callback?.Invoke(PurchaseResult.Failed("Модуль платежей не включён в PluginYG2."));
            return false;
#else
            if (!IsInitialized)
            {
                callback?.Invoke(PurchaseResult.Rejected("Каталог покупок PluginYG2 ещё не готов."));
                return false;
            }

            if (pendingCallback != null)
            {
                callback?.Invoke(PurchaseResult.Rejected("Покупка уже выполняется."));
                return false;
            }

            if (!TryGetProduct(productId, out _))
            {
                callback?.Invoke(PurchaseResult.Rejected($"Товар '{productId}' не найден в каталоге PluginYG2."));
                return false;
            }

            pendingProductId = productId;
            pendingCallback = callback;
            YG2.BuyPayments(productId);
            return true;
#endif
        }

        public IReadOnlyList<PurchaseProductInfo> GetProducts()
        {
            return products;
        }

#if Payments_yg
        private void OnGetPayments()
        {
            RefreshProductsFromPlugin();
        }

        private void OnPurchaseSuccess(string productId)
        {
            if (!IsPendingProduct(productId))
            {
                return;
            }

            CompletePurchase(PurchaseResult.Completed($"Покупка '{productId}' подтверждена PluginYG2."));
        }

        private void OnPurchaseFailed(string productId)
        {
            if (!IsPendingProduct(productId))
            {
                return;
            }

            CompletePurchase(PurchaseResult.Failed("PluginYG2 вернул неуспешный результат покупки. Отдельный callback отмены отсутствует."));
        }

        private void RefreshProductsFromPlugin()
        {
            products.Clear();

            Purchase[] pluginProducts = YG2.purchases;
            if (pluginProducts == null || pluginProducts.Length == 0)
            {
                catalogLoaded = false;
                return;
            }

            for (int i = 0; i < pluginProducts.Length; i++)
            {
                Purchase purchase = pluginProducts[i];
                if (purchase == null || string.IsNullOrWhiteSpace(purchase.id))
                {
                    continue;
                }

                products.Add(new PurchaseProductInfo(
                    purchase.id,
                    purchase.title,
                    purchase.description,
                    purchase.price,
                    purchase.priceValue,
                    purchase.priceCurrencyCode,
                    purchase.currencyImageURL,
                    purchase.consumed));
            }

            catalogLoaded = products.Count > 0;
        }

        private bool TryGetProduct(string productId, out PurchaseProductInfo productInfo)
        {
            for (int i = 0; i < products.Count; i++)
            {
                if (string.Equals(products[i].ProductId, productId, StringComparison.Ordinal))
                {
                    productInfo = products[i];
                    return true;
                }
            }

            productInfo = default;
            return false;
        }

        private bool IsPendingProduct(string productId)
        {
            return pendingCallback != null && string.Equals(pendingProductId, productId, StringComparison.Ordinal);
        }

        private void CompletePurchase(PurchaseResult result)
        {
            Action<PurchaseResult> callback = pendingCallback;
            pendingCallback = null;
            pendingProductId = string.Empty;
            callback?.Invoke(result);
        }
#endif
    }
}
