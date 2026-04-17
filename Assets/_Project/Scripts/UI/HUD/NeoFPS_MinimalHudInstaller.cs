using System;
using _Project.Scripts.Localization;
using NeoFPS;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.UI.HUD
{
    /// <summary>
    /// Создаёт минимальный HUD NeoFPS в Main-сцене и оставляет отдельные корни под PC / Mobile.
    /// </summary>
    public sealed class NeoFPS_MinimalHudInstaller : MonoBehaviour
    {
        [SerializeField] private GameObject crosshairPrefab;
        [SerializeField] private GameObject ammoCounterPrefab;
        [SerializeField] private GameObject inventoryHudPrefab;
        private const string HudRootName = "HUD_Root";
        private const string HudPcName = "HUD_PC";
        private const string HudMobileName = "HUD_Mobile";
        private const string CrosshairInstanceName = "Crosshair_PC";
        private const string InventoryInstanceName = "InventoryStandard_PC";
        private const string AmmoCounterInstanceName = "AmmoCounter_PC";
        private const string RescueCounterInstanceName = "RescueCounter_PC";

        private void Awake()
        {
            var canvasRoot = transform as RectTransform;
            if (canvasRoot == null)
            {
                Debug.LogError("NeoFPS_MinimalHudInstaller должен висеть на Canvas с RectTransform.", this);
                return;
            }

            if (canvasRoot.Find(HudRootName) != null)
                return;

            if (crosshairPrefab == null || ammoCounterPrefab == null || inventoryHudPrefab == null)
            {
                Debug.LogError("NeoFPS_MinimalHudInstaller не настроен: отсутствуют ссылки на HUD prefab.", this);
                return;
            }

            var hudRoot = CreateStretchChild(HudRootName, canvasRoot);
            hudRoot.gameObject.AddComponent<SoloPlayerCharacterEventWatcher>();
            hudRoot.SetSiblingIndex(0);

            var hudPc = CreateStretchChild(HudPcName, hudRoot);
            var hudMobile = CreateStretchChild(HudMobileName, hudRoot);
            hudMobile.gameObject.SetActive(false);

            CreateHudElement(crosshairPrefab, hudPc, CrosshairInstanceName);
            CreateHudElement(inventoryHudPrefab, hudPc, InventoryInstanceName);
            CreateHudElement(ammoCounterPrefab, hudPc, AmmoCounterInstanceName);
            CreateRescueCounter(hudPc);
        }

        private static RectTransform CreateStretchChild(string objectName, Transform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            return rectTransform;
        }

        private static void CreateHudElement(GameObject prefab, Transform parent, string instanceName)
        {
            var instance = Instantiate(prefab, parent, false);
            instance.name = instanceName;
        }

        private static void CreateRescueCounter(Transform parent)
        {
            var gameObject = new GameObject(RescueCounterInstanceName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(24f, -24f);
            rectTransform.sizeDelta = new Vector2(260f, 36f);

            var text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white;
            text.text = ProjectLocalizationYG.FormatRescueProgress(0, 0);

            var presenterType = Type.GetType("_Project.Scripts.UI.HUD.RescueHudPresenter, Assembly-CSharp");
            if (presenterType != null)
            {
                gameObject.AddComponent(presenterType);
            }
        }
    }
}
