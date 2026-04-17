using System.Collections;
using System.Collections.Generic;
using Modules.RescueObjective;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Project.Scripts.Gameplay.RescueTargets
{
    /// <summary>
    /// Запускает fade-out спасенного жителя и отключает его корневой объект после завершения.
    /// Это project-side visual helper и он не меняет ядро RescueObjective.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RescueObjective))]
    public sealed class RescueTargetFadeOutOnRescue : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Корневой объект, который будет отключен после завершения fade. Если не задан, отключается текущий объект.")]
        [SerializeField] private Transform rootToDisable;

        [Header("Тайминги")]
        [Tooltip("Небольшая задержка перед началом fade, чтобы анимация счастья успела читаться.")]
        [SerializeField, Min(0f)] private float fadeDelay = 0.2f;
        [Tooltip("Длительность плавного исчезновения.")]
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.9f;
        [Tooltip("Если включено, корневой объект отключается сразу после полного исчезновения.")]
        [SerializeField] private bool disableAfterFade = true;

        private readonly List<Material> runtimeMaterials = new();
        private readonly List<Color> initialColors = new();
        private readonly List<int> colorPropertyIds = new();

        private RescueObjective objective;
        private RescueObjectiveState lastKnownState;
        private Coroutine fadeRoutine;
        private bool stateInitialized;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        private static readonly int ModeId = Shader.PropertyToID("_Mode");

        private void Awake()
        {
            objective = GetComponent<RescueObjective>();
            if (rootToDisable == null)
            {
                rootToDisable = transform;
            }
        }

        private void OnEnable()
        {
            stateInitialized = false;
            SyncState();
        }

        private void OnDisable()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
        }

        private void Update()
        {
            if (objective == null)
            {
                return;
            }

            if (!stateInitialized || objective.State != lastKnownState)
            {
                SyncState();
            }
        }

        private void SyncState()
        {
            if (objective == null)
            {
                return;
            }

            lastKnownState = objective.State;
            stateInitialized = true;

            if (lastKnownState != RescueObjectiveState.Rescued || fadeRoutine != null)
            {
                return;
            }

            fadeRoutine = StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator FadeOutRoutine()
        {
            CacheActiveRuntimeMaterials();

            if (runtimeMaterials.Count == 0)
            {
                DisableRoot();
                fadeRoutine = null;
                yield break;
            }

            if (fadeDelay > 0f)
            {
                yield return new WaitForSeconds(fadeDelay);
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / fadeDuration));
                ApplyAlpha(alpha);
                yield return null;
            }

            ApplyAlpha(0f);
            DisableRoot();
            fadeRoutine = null;
        }

        private void CacheActiveRuntimeMaterials()
        {
            runtimeMaterials.Clear();
            initialColors.Clear();
            colorPropertyIds.Clear();

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Material[] materials = renderer.materials;
                foreach (Material material in materials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    int colorPropertyId = ResolveColorProperty(material);
                    if (colorPropertyId == 0)
                    {
                        continue;
                    }

                    PrepareMaterialForFade(material);
                    runtimeMaterials.Add(material);
                    initialColors.Add(material.GetColor(colorPropertyId));
                    colorPropertyIds.Add(colorPropertyId);
                }
            }
        }

        private static int ResolveColorProperty(Material material)
        {
            if (material.HasProperty(BaseColorId))
            {
                return BaseColorId;
            }

            if (material.HasProperty(ColorId))
            {
                return ColorId;
            }

            return 0;
        }

        private static void PrepareMaterialForFade(Material material)
        {
            if (material.HasProperty(SurfaceId))
            {
                material.SetFloat(SurfaceId, 1f);
            }

            if (material.HasProperty(BlendId))
            {
                material.SetFloat(BlendId, 0f);
            }

            if (material.HasProperty(SrcBlendId))
            {
                material.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty(DstBlendId))
            {
                material.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty(ZWriteId))
            {
                material.SetFloat(ZWriteId, 0f);
            }

            if (material.HasProperty(ModeId))
            {
                material.SetFloat(ModeId, 2f);
            }

            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        private void ApplyAlpha(float alpha)
        {
            for (int i = 0; i < runtimeMaterials.Count; i++)
            {
                Material material = runtimeMaterials[i];
                if (material == null)
                {
                    continue;
                }

                Color color = initialColors[i];
                color.a = alpha;
                material.SetColor(colorPropertyIds[i], color);
            }
        }

        private void DisableRoot()
        {
            if (!disableAfterFade)
            {
                return;
            }

            Transform targetRoot = rootToDisable != null ? rootToDisable : transform;
            if (targetRoot != null)
            {
                targetRoot.gameObject.SetActive(false);
            }
        }
    }
}
