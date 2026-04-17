using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.PurchasesCore
{
    /// <summary>
    /// Reusable фасад внутриигровых покупок.
    /// Отвечает только за orchestration purchase-flow и результат покупки.
    /// </summary>
    public sealed class PurchaseService
    {
        private static PurchaseService instance;

        private readonly PurchaseProjectSettings settings = new();
        private readonly IPurchaseProvider provider;
        private bool purchaseRequestInProgress;

        private PurchaseService()
        {
            provider = CreateProvider(settings.ProviderType);
            provider.Initialize();
        }

        public static PurchaseService Instance => instance ??= new PurchaseService();

        public string ActiveProviderId => provider.ProviderId;
        public bool IsInitialized => provider.IsInitialized;
        public bool IsPurchaseInProgress => purchaseRequestInProgress || provider.IsPurchaseInProgress;

        public void Warmup()
        {
            // Явная точка ранней инициализации из bootstrap.
        }

        public IReadOnlyList<PurchaseProductInfo> GetProducts()
        {
            return provider.GetProducts();
        }

        public bool TryGetProduct(string productId, out PurchaseProductInfo productInfo)
        {
            IReadOnlyList<PurchaseProductInfo> products = provider.GetProducts();
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

        public bool CanPurchase(string productId, out string reason)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                reason = "Идентификатор товара пустой.";
                return false;
            }

            if (IsPurchaseInProgress)
            {
                reason = "Покупка уже выполняется.";
                return false;
            }

            if (!provider.IsInitialized)
            {
                reason = "Каталог покупок ещё не готов.";
                return false;
            }

            if (!TryGetProduct(productId, out _))
            {
                reason = $"Товар '{productId}' не найден в каталоге PluginYG2.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryPurchase(string productId)
        {
            return TryPurchase(productId, null);
        }

        public bool TryPurchase(string productId, Action<PurchaseResult> callback)
        {
            if (!CanPurchase(productId, out string reason))
            {
                Debug.Log($"PurchaseService: purchase '{productId}' отклонён. Причина: {reason}");
                callback?.Invoke(PurchaseResult.Rejected(reason));
                return false;
            }

            if (!provider.TryPurchase(productId, result => OnPurchaseCompleted(productId, result, callback)))
            {
                return false;
            }

            purchaseRequestInProgress = true;
            return true;
        }

        private void OnPurchaseCompleted(string productId, PurchaseResult result, Action<PurchaseResult> callback)
        {
            purchaseRequestInProgress = false;

            if (!result.IsSuccess)
            {
                Debug.Log($"PurchaseService: purchase '{productId}' завершён со статусом {result.Status}. {result.Message}");
            }

            callback?.Invoke(result);
        }

        private static IPurchaseProvider CreateProvider(PurchaseProviderType providerType)
        {
            return providerType switch
            {
                PurchaseProviderType.YandexGames => new YandexPurchaseProvider(),
                _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, "Неизвестный purchase provider.")
            };
        }
    }
}
