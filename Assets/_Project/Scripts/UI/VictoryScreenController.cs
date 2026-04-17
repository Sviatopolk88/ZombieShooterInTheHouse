using System;
using _Project.Scripts.GameFlow;
using _Project.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

namespace _Project.Scripts.UI
{
    public sealed class VictoryScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private TMP_Text rescuedLabel;
        [SerializeField] private TMP_Text failedLabel;
        [SerializeField] private TMP_Text remainingLabel;
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private TMP_Text nextButtonLabel;
        [SerializeField] private TMP_Text restartButtonLabel;

        private Action nextAction;
        private int rescuedCount;
        private int totalCount;
        private int failedCount;
        private int remainingCount;
        private bool showNextButton = true;
        private bool showRestartButton = true;
        private bool showBossSubtitle = true;

        private void Reset()
        {
            AutoAssignReferences();
            RefreshLocalizedText();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            AutoAssignReferences();
            RefreshLocalizedText();
        }

        private void Awake()
        {
            AutoAssignReferences();
            BindButtons();
            RefreshLocalizedText();

            if (root != null && Application.isPlaying)
            {
                root.SetActive(false);
            }
        }

        private void OnEnable()
        {
            YG2.onSwitchLang += OnSwitchLanguage;
            RefreshLocalizedText();
        }

        private void OnDisable()
        {
            YG2.onSwitchLang -= OnSwitchLanguage;
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        public void Show(int rescued, int total, int failed, int remaining)
        {
            rescuedCount = Mathf.Max(0, rescued);
            totalCount = Mathf.Max(0, total);
            failedCount = Mathf.Max(0, failed);
            remainingCount = Mathf.Max(0, remaining);

            if (root != null)
            {
                root.SetActive(true);
            }

            RefreshLocalizedText();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public void SetNextAction(Action action)
        {
            nextAction = action;
        }

        public void SetButtonsVisible(bool showNext, bool showRestart)
        {
            showNextButton = showNext;
            showRestartButton = showRestart;

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(showNextButton);
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(showRestartButton);
            }
        }

        public void SetBossDefeatedVisible(bool visible)
        {
            showBossSubtitle = visible;

            if (subtitleLabel != null)
            {
                subtitleLabel.gameObject.SetActive(showBossSubtitle);
            }
        }

        private void BindButtons()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNext);
                nextButton.onClick.AddListener(HandleNext);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestart);
                restartButton.onClick.AddListener(HandleRestart);
            }
        }

        private void UnbindButtons()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNext);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestart);
            }
        }

        private void HandleNext()
        {
            if (nextAction != null)
            {
                nextAction.Invoke();
                return;
            }

            Debug.LogWarning("VictoryScreenController: next action is not assigned.", this);
        }

        private void HandleRestart()
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            GameFlowService.Instance?.RestartLevel();
        }

        private void RefreshLocalizedText()
        {
            if (titleLabel != null)
            {
                titleLabel.text = ProjectLocalizationYG.Get(ProjectTextKey.VictoryTitle);
            }

            if (subtitleLabel != null)
            {
                subtitleLabel.text = ProjectLocalizationYG.Get(ProjectTextKey.VictorySubtitle);
                subtitleLabel.gameObject.SetActive(showBossSubtitle);
            }

            if (rescuedLabel != null)
            {
                rescuedLabel.text = ProjectLocalizationYG.FormatVictoryRescued(rescuedCount, totalCount);
            }

            if (failedLabel != null)
            {
                failedLabel.text = ProjectLocalizationYG.FormatVictoryFailed(failedCount);
            }

            if (remainingLabel != null)
            {
                remainingLabel.text = ProjectLocalizationYG.FormatVictoryRemaining(remainingCount);
            }

            if (summaryLabel != null)
            {
                summaryLabel.text = remainingCount <= 0
                    ? ProjectLocalizationYG.Get(ProjectTextKey.VictorySummaryPerfect)
                    : ProjectLocalizationYG.Get(ProjectTextKey.VictorySummaryIncomplete);
            }

            if (nextButtonLabel != null)
            {
                nextButtonLabel.text = ProjectLocalizationYG.Get(ProjectTextKey.VictoryNext);
            }

            if (restartButtonLabel != null)
            {
                restartButtonLabel.text = ProjectLocalizationYG.Get(ProjectTextKey.VictoryRestart);
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(showNextButton);
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(showRestartButton);
            }
        }

        private void AutoAssignReferences()
        {
            if (root == null)
            {
                root = gameObject;
            }

            titleLabel ??= FindLabel("Panel/Header/TitleText");
            subtitleLabel ??= FindLabel("Panel/Header/SubtitleText");
            rescuedLabel ??= FindLabel("Panel/Stats/RescuedText");
            failedLabel ??= FindLabel("Panel/Stats/FailedText");
            remainingLabel ??= FindLabel("Panel/Stats/RemainingText");
            summaryLabel ??= FindLabel("Panel/Stats/SummaryText");
            nextButton ??= FindButton("Panel/Buttons/NextButton");
            restartButton ??= FindButton("Panel/Buttons/RestartButton");
            nextButtonLabel ??= FindLabel("Panel/Buttons/NextButton/Label");
            restartButtonLabel ??= FindLabel("Panel/Buttons/RestartButton/Label");
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

        private void OnSwitchLanguage(string language)
        {
            RefreshLocalizedText();
        }
    }
}
