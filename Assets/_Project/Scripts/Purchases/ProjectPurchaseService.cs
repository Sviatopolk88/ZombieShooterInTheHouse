using System;
using Modules.PurchasesCore;
using UnityEngine;

namespace _Project.Scripts.Purchases
{
    /// <summary>
    /// Project-side bridge между reusable purchase-core и gameplay-наградами за покупку.
    /// </summary>
    public static class ProjectPurchaseService
    {
        public static bool CanPurchase(string productId, out string reason)
        {
            if (!PurchaseService.Instance.CanPurchase(productId, out reason))
            {
                return false;
            }

            if (ProjectPurchasesOwnershipStore.IsOwned(productId))
            {
                reason = "Товар уже куплен и разблокирован.";
                return false;
            }

            return PurchaseRewardApplier.CanApplyRuntime(productId, out reason);
        }

        public static bool TryPurchase(string productId)
        {
            return TryPurchase(productId, null);
        }

        public static bool TryPurchase(string productId, Action<PurchaseResult> callback)
        {
            if (!CanPurchase(productId, out string reason))
            {
                Debug.Log($"ProjectPurchaseService: purchase '{productId}' отклонён. Причина: {reason}");
                callback?.Invoke(PurchaseResult.Rejected(reason));
                return false;
            }

            return PurchaseService.Instance.TryPurchase(productId, result => OnPurchaseCompleted(productId, result, callback));
        }

        public static bool TryGetProduct(string productId, out PurchaseProductInfo productInfo)
        {
            return PurchaseService.Instance.TryGetProduct(productId, out productInfo);
        }

        public static void RestoreOwnedPurchases()
        {
            PurchaseRewardApplier.RestoreOwnedPurchases(ProjectPurchasesOwnershipStore.GetOwnedProductIds());
        }

        private static void OnPurchaseCompleted(string productId, PurchaseResult result, Action<PurchaseResult> callback)
        {
            if (!result.IsSuccess)
            {
                callback?.Invoke(result);
                return;
            }

            if (!ProjectPurchasesOwnershipStore.TryGrantOwnership(productId, out string normalizedProductId, out string ownershipMessage))
            {
                Debug.LogWarning($"ProjectPurchaseService: purchase '{productId}' подтверждён, но entitlement не сохранён. {ownershipMessage}");
                callback?.Invoke(PurchaseResult.Failed(ownershipMessage));
                return;
            }

            if (!PurchaseRewardApplier.TryApplyOwnedProduct(normalizedProductId, out string rewardMessage))
            {
                string deferredMessage = CombineMessages(
                    ownershipMessage,
                    rewardMessage,
                    "Выдача будет повторена автоматически при готовности gameplay.");

                Debug.LogWarning($"ProjectPurchaseService: purchase '{productId}' подтверждён, entitlement сохранён, но runtime-выдача отложена. {deferredMessage}");
                callback?.Invoke(PurchaseResult.Completed(deferredMessage));
                return;
            }

            string successMessage = CombineMessages(ownershipMessage, rewardMessage);
            Debug.Log($"ProjectPurchaseService: purchase '{productId}' обработан успешно. {successMessage}");
            callback?.Invoke(PurchaseResult.Completed(successMessage));
        }

        private static string CombineMessages(params string[] parts)
        {
            System.Text.StringBuilder builder = new();

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(part.Trim());
            }

            return builder.ToString();
        }
    }
}
