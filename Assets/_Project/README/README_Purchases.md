# Purchases Module

## PluginYG2 Audit

PluginYG2 purchases use these runtime points:

- `YG2.BuyPayments(string id)`
- `YG2.purchases`
- `YG2.onGetPayments`
- `YG2.onPurchaseSuccess`
- `YG2.onPurchaseFailed`

Catalog loading:

- product list is filled through `ysdk.getPayments().getCatalog()`
- existing unconsumed purchases are checked through `payments.getPurchases()`
- catalog arrives in Unity through `YG2.onGetPayments`

Purchase flow:

- purchase starts with `YG2.BuyPayments(productId)`
- in Yandex JS bridge success path immediately calls `ConsumePurchase(...)`
- only after consume the plugin raises `OnPurchaseSuccess`
- failure and user cancellation both end up in `OnPurchaseFailed`

Important limitation:

- PluginYG2 does not provide a dedicated cancellation callback for purchases
- for this reason `Cancelled` exists in purchase-core result model, but current `YandexPurchaseProvider` can only reliably emit `Completed`, `Failed`, or `Rejected`

## Architecture

Reusable purchase-core:

- `Assets/Modules/PurchasesCore/Core/PurchaseService.cs`
- `Assets/Modules/PurchasesCore/Core/IPurchaseProvider.cs`
- `Assets/Modules/PurchasesCore/Core/PurchaseResult.cs`
- `Assets/Modules/PurchasesCore/Core/PurchaseEnums.cs`
- `Assets/Modules/PurchasesCore/Core/PurchaseProductInfo.cs`
- `Assets/Modules/PurchasesCore/Core/PurchaseProjectSettings.cs`
- `Assets/Modules/PurchasesCore/Providers/Yandex/YandexPurchaseProvider.cs`

Project-side integration:

- `Assets/_Project/Scripts/Purchases/ProjectPurchaseService.cs`
- `Assets/_Project/Scripts/Purchases/PurchaseRewardApplier.cs`
- `Assets/_Project/Scripts/Purchases/PurchaseOfferZone.cs`
- `Assets/_Project/Scripts/Purchases/ProjectPurchasesEntitlementSync.cs`
- `Assets/_Project/Scripts/Purchases/ProjectPurchasesSaveData.cs`

Dependency direction:

- `_Project` depends on `Modules.PurchasesCore`
- `Modules.PurchasesCore` does not depend on `_Project`
- `YandexPurchaseProvider` depends on PluginYG2 / YG2

## First Product

MVP product reward:

- product id `weapon_shotgun` -> grant shotgun

Compatibility alias for current PluginYG2 editor simulation:

- product id `gun` -> also grants shotgun

Why alias is needed:

- current PluginYG2 demo catalog in editor contains `gun`
- production Yandex catalog should use a clearer id such as `weapon_shotgun`

## Entitlement Handling

PluginYG2 consume flow makes the purchase effectively consumable at SDK level.

To keep a real-money weapon purchase stable across reloads, project-side code stores entitlement in:

- `YG2.saves.purchasedWeaponShotgun`

After successful purchase:

- shotgun is granted to inventory
- entitlement is saved through `YG2.SaveProgress()`

After loading `Level_1`:

- `ProjectPurchasesEntitlementSync` re-applies already owned purchases to the player inventory

## Weapon Grant

Project-side reward logic does not live in purchase-core.

Current shotgun grant uses:

- NeoFPS `IInventory.AddItemFromPrefab(...)`
- project-side resource prefab:
  - `Assets/_Project/Resources/Purchases/Firearm_Shotgun_Quickswitch_Purchase.prefab`

This keeps purchase-core free from gameplay dependencies while still allowing project code to decide what a product gives.

## UI / Trigger Point

Current MVP trigger point:

- `PurchaseOfferZone`

Setup on scene:

1. Create a GameObject, for example `PurchaseZone_Shotgun`.
2. Add a `Collider` and enable `Is Trigger`.
3. Add `PurchaseOfferZone`.
4. Set `productId`.
   - for editor simulation you can use `gun`
   - for production Yandex catalog prefer `weapon_shotgun`
5. Optionally assign `promptRoot` and `promptLabel`.
6. Place the zone in `Level_1` manually if needed.

`PurchaseOfferZone` never calls YG2 directly. It uses only `ProjectPurchaseService`.

## How To Add A New Product

1. Add the product to Yandex / PluginYG2 catalog.
2. Add project-side mapping in `PurchaseRewardApplier`.
3. If the product must persist, extend `SavesYG` in project-side code.
4. Add a new zone or UI entry that calls `ProjectPurchaseService.TryPurchase(productId)`.

## What To Check Manually

1. PluginYG2 purchase catalog becomes available and `PurchaseService.Instance.IsInitialized` turns true.
2. `PurchaseOfferZone` shows product title / price when catalog is loaded.
3. Successful purchase of `gun` or `weapon_shotgun` grants the shotgun.
4. Repeating the same purchase after entitlement is saved is blocked.
5. After level reload or new session, owned shotgun is restored from `YG2.saves`.
6. Purchase failure or user cancellation does not grant the weapon.
7. `Level_2` was not changed.
