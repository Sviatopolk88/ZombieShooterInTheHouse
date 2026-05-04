using System;
using System.Collections.Generic;
using YG;

namespace _Project.Scripts.Purchases
{
    /// <summary>
    /// Project-side ownership storage поверх YG2.saves.
    /// Хранит купленные товары в едином списке, нормализует productId и мигрирует legacy-флаги.
    /// </summary>
    public static class ProjectPurchasesOwnershipStore
    {
        public static bool IsOwned(string productId)
        {
#if !Storage_yg
            return false;
#else
            if (!TryNormalizeStoredProductId(productId, out string normalizedProductId))
            {
                return false;
            }

            EnsureStorage();
            MigrateLegacyData();
            return ContainsOwnedProduct(normalizedProductId);
#endif
        }

        public static bool TryGrantOwnership(string productId, out string normalizedProductId, out string message)
        {
            normalizedProductId = string.Empty;

#if !Storage_yg
            message = "Модуль storage в PluginYG2 не включён.";
            return false;
#else
            if (!TryNormalizeStoredProductId(productId, out normalizedProductId))
            {
                message = $"Неизвестный товар '{productId}'.";
                return false;
            }

            EnsureStorage();

            bool dataChanged = MigrateLegacyData();
            if (!ContainsOwnedProduct(normalizedProductId))
            {
                YG2.saves.purchasedProductIds.Add(normalizedProductId);
                dataChanged = true;
                message = $"Entitlement для '{normalizedProductId}' сохранён.";
            }
            else
            {
                message = $"Entitlement для '{normalizedProductId}' уже был сохранён.";
            }

            if (dataChanged && YG2.isSDKEnabled)
            {
                YG2.SaveProgress();
            }

            return true;
#endif
        }

        public static IReadOnlyList<string> GetOwnedProductIds()
        {
#if !Storage_yg
            return Array.Empty<string>();
#else
            EnsureStorage();
            bool dataChanged = MigrateLegacyData();
            dataChanged |= SanitizeOwnedProducts();

            if (dataChanged && YG2.isSDKEnabled)
            {
                YG2.SaveProgress();
            }

            return YG2.saves.purchasedProductIds;
#endif
        }

        public static bool TryResetForDevelopment(out string message)
        {
#if !Storage_yg
            message = "Модуль storage в PluginYG2 не включён.";
            return false;
#else
            EnsureStorage();

            bool dataChanged = false;

            if (YG2.saves.purchasedProductIds != null && YG2.saves.purchasedProductIds.Count > 0)
            {
                YG2.saves.purchasedProductIds.Clear();
                dataChanged = true;
            }

            if (YG2.saves.purchasedWeaponShotgun)
            {
                YG2.saves.purchasedWeaponShotgun = false;
                dataChanged = true;
            }

            if (YG2.saves.purchasesOwnershipMigrated)
            {
                YG2.saves.purchasesOwnershipMigrated = false;
                dataChanged = true;
            }

            if (dataChanged && YG2.isSDKEnabled)
            {
                YG2.SaveProgress();
            }

            message = dataChanged
                ? "Project-side ownership очищен."
                : "Project-side ownership уже был пуст.";
            return true;
#endif
        }

#if Storage_yg
        private static void EnsureStorage()
        {
            if (YG2.saves == null)
            {
                YG2.saves = new SavesYG();
            }

            YG2.saves.purchasedProductIds ??= new List<string>();
        }

        private static bool MigrateLegacyData()
        {
            if (YG2.saves.purchasesOwnershipMigrated)
            {
                return false;
            }

            bool dataChanged = true;

            if (YG2.saves.purchasedWeaponShotgun)
            {
                string normalizedShotgunId = NormalizeId(PurchaseRewardApplier.ShotgunProductId);
                if (!string.IsNullOrEmpty(normalizedShotgunId) && !ContainsOwnedProduct(normalizedShotgunId))
                {
                    YG2.saves.purchasedProductIds.Add(normalizedShotgunId);
                    dataChanged = true;
                }
            }

            YG2.saves.purchasesOwnershipMigrated = true;
            return dataChanged;
        }

        private static bool SanitizeOwnedProducts()
        {
            List<string> ownedProductIds = YG2.saves.purchasedProductIds;
            if (ownedProductIds == null)
            {
                YG2.saves.purchasedProductIds = new List<string>();
                return true;
            }

            bool dataChanged = false;
            List<string> normalizedOwnedProducts = new(ownedProductIds.Count);

            for (int i = 0; i < ownedProductIds.Count; i++)
            {
                if (!TryNormalizeStoredProductId(ownedProductIds[i], out string normalizedProductId))
                {
                    dataChanged = true;
                    continue;
                }

                if (Contains(normalizedOwnedProducts, normalizedProductId))
                {
                    dataChanged = true;
                    continue;
                }

                normalizedOwnedProducts.Add(normalizedProductId);
                if (!string.Equals(ownedProductIds[i], normalizedProductId, StringComparison.Ordinal))
                {
                    dataChanged = true;
                }
            }

            if (dataChanged)
            {
                ownedProductIds.Clear();
                ownedProductIds.AddRange(normalizedOwnedProducts);
            }

            return dataChanged;
        }

        private static bool ContainsOwnedProduct(string normalizedProductId)
        {
            return Contains(YG2.saves.purchasedProductIds, normalizedProductId);
        }

        private static bool Contains(List<string> productIds, string normalizedProductId)
        {
            if (productIds == null || string.IsNullOrEmpty(normalizedProductId))
            {
                return false;
            }

            for (int i = 0; i < productIds.Count; i++)
            {
                if (string.Equals(NormalizeId(productIds[i]), normalizedProductId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
#endif

        private static bool TryNormalizeStoredProductId(string productId, out string normalizedProductId)
        {
            if (!PurchaseRewardApplier.TryNormalizeOwnedProductId(productId, out string canonicalProductId))
            {
                normalizedProductId = string.Empty;
                return false;
            }

            normalizedProductId = NormalizeId(canonicalProductId);
            return !string.IsNullOrEmpty(normalizedProductId);
        }

        private static string NormalizeId(string productId)
        {
            return string.IsNullOrWhiteSpace(productId)
                ? string.Empty
                : productId.Trim().ToLowerInvariant();
        }
    }
}
