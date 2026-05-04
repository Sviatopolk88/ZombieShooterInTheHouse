using System.Collections.Generic;
using _Project.Scripts.Localization;
using Modules.PurchasesCore;
using TMPro;
using UnityEngine;
using YG;

namespace _Project.Scripts.Purchases
{
    /// <summary>
    /// Reusable project-side зона покупки, которая вызывает purchase-core через project-side bridge.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class PurchaseOfferZone : MonoBehaviour
    {
        [Header("Product")]
        [Tooltip("Идентификатор товара в каталоге покупок.")]
        [SerializeField] private string productId = PurchaseRewardApplier.ShotgunEditorSimulationProductId;

        [Header("Activation")]
        [Tooltip("Клавиша взаимодействия с зоной покупки.")]
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [Tooltip("Слои объектов, которые могут активировать зону покупки.")]
        [SerializeField] private LayerMask activatorLayers = ~0;
        [Tooltip("Тег объекта, которому разрешено активировать зону покупки.")]
        [SerializeField] private string requiredTag = "Player";

        [Header("Prompt")]
        [Tooltip("Корневой объект визуальной подсказки о покупке.")]
        [SerializeField] private GameObject promptRoot;
        [Tooltip("Текстовый компонент, в который выводится название и цена товара.")]
        [SerializeField] private TMP_Text promptLabel;

        private readonly HashSet<Collider> occupants = new();
        private bool requestPending;

        private void Reset()
        {
            Collider zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private void OnValidate()
        {
            Collider zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            YG2.onSwitchLang += OnSwitchLanguage;
            RefreshPrompt();
        }

        private void OnDisable()
        {
            YG2.onSwitchLang -= OnSwitchLanguage;
            occupants.Clear();
            requestPending = false;
            RefreshPrompt();
        }

        private void Update()
        {
            if (occupants.Count == 0)
            {
                RefreshPrompt();
                return;
            }

            RefreshPrompt();

            if (requestPending || !UnityEngine.Input.GetKeyDown(interactionKey))
            {
                return;
            }

            TryRequestPurchase();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!MatchesActivator(other))
            {
                return;
            }

            occupants.Add(other);
            RefreshPrompt();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null)
            {
                return;
            }

            occupants.Remove(other);
            RefreshPrompt();
        }

        private bool MatchesActivator(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            GameObject candidate = other.gameObject;
            if (((1 << candidate.layer) & activatorLayers.value) == 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(requiredTag) && !candidate.CompareTag(requiredTag))
            {
                return false;
            }

            return true;
        }

        private void TryRequestPurchase()
        {
            if (!ProjectPurchaseService.TryPurchase(productId, OnPurchaseCompleted))
            {
                RefreshPrompt();
                return;
            }

            requestPending = true;
            RefreshPrompt();
        }

        private void OnPurchaseCompleted(PurchaseResult result)
        {
            requestPending = false;
            RefreshPrompt();
        }

        private void RefreshPrompt()
        {
            if (promptRoot != null)
            {
                promptRoot.SetActive(occupants.Count > 0);
            }

            if (promptLabel == null)
            {
                return;
            }

            if (occupants.Count == 0)
            {
                promptLabel.text = string.Empty;
                return;
            }

            promptLabel.text = BuildPromptText();
        }

        private string BuildPromptText()
        {
            if (requestPending)
            {
                return ProjectLocalizationYG.Get(ProjectTextKey.PurchaseRequest);
            }

            string productTitle = GetProductTitle();
            string productPrice = GetProductPrice();

            if (ProjectPurchaseService.CanPurchase(productId, out string reason))
            {
                if (string.IsNullOrWhiteSpace(productPrice))
                {
                    return ProjectLocalizationYG.FormatPurchaseBuy(interactionKey, productTitle);
                }

                return ProjectLocalizationYG.FormatPurchaseBuyWithPrice(interactionKey, productTitle, productPrice);
            }

            return ProjectLocalizationYG.FormatPurchaseUnavailable(productTitle, reason);
        }

        private string GetProductTitle()
        {
            if (ProjectPurchaseService.TryGetProduct(productId, out PurchaseProductInfo productInfo)
                && !string.IsNullOrWhiteSpace(productInfo.Title))
            {
                return productInfo.Title;
            }

            return ProjectLocalizationYG.Get(ProjectTextKey.PurchaseFallbackTitle);
        }

        private string GetProductPrice()
        {
            if (ProjectPurchaseService.TryGetProduct(productId, out PurchaseProductInfo productInfo))
            {
                return productInfo.Price;
            }

            return string.Empty;
        }

        private void OnSwitchLanguage(string language)
        {
            RefreshPrompt();
        }
    }
}
