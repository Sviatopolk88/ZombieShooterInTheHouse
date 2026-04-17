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

            return PurchaseRewardApplier.CanApplyPurchase(productId, out reason);
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
            PurchaseRewardApplier.RestoreOwnedPurchases();
        }

        private static void OnPurchaseCompleted(string productId, PurchaseResult result, Action<PurchaseResult> callback)
        {
            if (!result.IsSuccess)
            {
                callback?.Invoke(result);
                return;
            }

            if (!PurchaseRewardApplier.TryApplyPurchase(productId, out string rewardMessage))
            {
                Debug.LogWarning($"ProjectPurchaseService: purchase '{productId}' подтверждён, но награда не выдана. {rewardMessage}");
                callback?.Invoke(PurchaseResult.Failed(rewardMessage));
                return;
            }

            Debug.Log($"ProjectPurchaseService: purchase '{productId}' обработан успешно. {rewardMessage}");
            callback?.Invoke(PurchaseResult.Completed(rewardMessage));
        }
    }
}
