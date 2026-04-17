using System;
using _Project.Scripts.GameFlow;
using _Project.Scripts.Localization;
using _Project.Scripts.Systems.SceneFlow;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YG;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Project-side окно подтверждения выхода, если на уровне остались не спасённые жители.
    /// Основной путь настройки: явный scene object в Main.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelExitConfirmationController : MonoBehaviour
    {
        private const string RootObjectName = "LevelExitConfirmationController";

        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button stayButton;
        [SerializeField] private TMP_Text leaveLabel;
        [SerializeField] private TMP_Text stayLabel;

        private Action confirmAction;
        private Action cancelAction;
        private int currentRemainingCount;

        public static LevelExitConfirmationController Instance { get; private set; }

        public static LevelExitConfirmationController EnsureInstance()
        {
            if (Instance != null)
            {
                Instance.EnsureUiReady();
                return Instance;
            }

            Scene mainScene = SceneManager.GetSceneByName(ProjectSceneNames.Main);
            if (!mainScene.IsValid() || !mainScene.isLoaded)
            {
                return null;
            }

            return EnsureInstanceInScene(mainScene);
        }

        private void Reset()
        {
            AutoAssignReferences();
            RefreshText();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            AutoAssignReferences();
            RefreshText();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            AutoAssignReferences();
            BindButtons();

            if (root != null && Application.isPlaying)
            {
                root.SetActive(false);
            }
        }

        private void OnEnable()
        {
            YG2.onSwitchLang += OnSwitchLanguage;
            RefreshText();
        }

        private void OnDestroy()
        {
            YG2.onSwitchLang -= OnSwitchLanguage;
            UnbindButtons();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Show(int remainingCount, Action onConfirm, Action onCancel)
        {
            currentRemainingCount = Mathf.Max(0, remainingCount);
            confirmAction = onConfirm;
            cancelAction = onCancel;

            if (!EnsureUiReady())
            {
                Debug.LogWarning("LevelExitConfirmationController: confirmation UI is not ready.", this);
                return;
            }

            RefreshText();
            root.SetActive(true);
            Time.timeScale = 0f;
            CursorStateService.Instance?.SetUiMode();
        }

        public void HideImmediately()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void OnLeaveClicked()
        {
            Action action = confirmAction;
            confirmAction = null;
            cancelAction = null;

            HideImmediately();
            Time.timeScale = 1f;
            CursorStateService.Instance?.SetGameplayMode();
            action?.Invoke();
        }

        private void OnStayClicked()
        {
            Action action = cancelAction;
            confirmAction = null;
            cancelAction = null;

            HideImmediately();
            Time.timeScale = 1f;
            CursorStateService.Instance?.SetGameplayMode();
            action?.Invoke();
        }

        private void RefreshText()
        {
            if (titleLabel != null)
            {
                titleLabel.text = ProjectLocalizationYG.Get(ProjectTextKey.ExitConfirmTitle);
            }

            if (messageLabel != null)
            {
                messageLabel.text = ProjectLocalizationYG.FormatExitConfirmationMessage(currentRemainingCount);
            }

            if (leaveLabel != null)
            {
                leaveLabel.text = ProjectLocalizationYG.Get(ProjectTextKey.ExitConfirmLeave);
            }

            if (stayLabel != null)
            {
                stayLabel.text = ProjectLocalizationYG.Get(ProjectTextKey.ExitConfirmStay);
            }
        }

        private void OnSwitchLanguage(string language)
        {
            RefreshText();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.name != ProjectSceneNames.Main)
            {
                return;
            }

            EnsureInstanceInScene(scene);
        }

        private static GameObject FindRootInScene(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == RootObjectName)
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static LevelExitConfirmationController EnsureInstanceInScene(Scene scene)
        {
            if (Instance != null)
            {
                Instance.EnsureUiReady();
                return Instance;
            }

            GameObject existingRoot = FindRootInScene(scene);
            if (existingRoot == null)
            {
                Debug.LogWarning("LevelExitConfirmationController: scene object not found in Main scene.");
                return null;
            }

            LevelExitConfirmationController existingController = existingRoot.GetComponent<LevelExitConfirmationController>();
            if (existingController == null)
            {
                Debug.LogWarning("LevelExitConfirmationController: component not found on scene object.", existingRoot);
                return null;
            }

            Instance = existingController;
            existingController.EnsureUiReady();
            return existingController;
        }

        private bool EnsureUiReady()
        {
            AutoAssignReferences();
            return root != null
                && titleLabel != null
                && messageLabel != null
                && leaveButton != null
                && stayButton != null
                && leaveLabel != null
                && stayLabel != null;
        }

        private void AutoAssignReferences()
        {
            root ??= gameObject;
            titleLabel ??= FindLabel("Panel/Header/TitleText");
            messageLabel ??= FindLabel("Panel/Message/MessageText");
            leaveButton ??= FindButton("Panel/Buttons/LeaveButton");
            stayButton ??= FindButton("Panel/Buttons/StayButton");
            leaveLabel ??= FindLabel("Panel/Buttons/LeaveButton/Label");
            stayLabel ??= FindLabel("Panel/Buttons/StayButton/Label");
        }

        private void BindButtons()
        {
            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveListener(OnLeaveClicked);
                leaveButton.onClick.AddListener(OnLeaveClicked);
            }

            if (stayButton != null)
            {
                stayButton.onClick.RemoveListener(OnStayClicked);
                stayButton.onClick.AddListener(OnStayClicked);
            }
        }

        private void UnbindButtons()
        {
            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveListener(OnLeaveClicked);
            }

            if (stayButton != null)
            {
                stayButton.onClick.RemoveListener(OnStayClicked);
            }
        }

        private TMP_Text FindLabel(string path)
        {
            Transform target = transform.Find(path);
            return target != null ? target.GetComponent<TMP_Text>() : null;
        }

        private Button FindButton(string path)
        {
            Transform target = transform.Find(path);
            return target != null ? target.GetComponent<Button>() : null;
        }
    }
}
