using System;
using _Project.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

namespace _Project.Scripts.UI
{
    public sealed class VictoryScreenController : MonoBehaviour
    {
        private const string RestartButtonPath = "Panel/Buttons/RestartButton";

        [Tooltip("Корневой объект экрана результата, который включается при завершении уровня и скрывается при переходе дальше.")]
        [SerializeField] private GameObject root;

        [Tooltip("Кнопка продолжения, запускающая переход на следующий уровень.")]
        [SerializeField] private Button nextButton;

        [Tooltip("Заголовок экрана результата с сообщением о прохождении уровня.")]
        [SerializeField] private TMP_Text titleLabel;

        [Tooltip("Подзаголовок результата, который можно скрывать, если победа над боссом не должна показываться.")]
        [SerializeField] private TMP_Text subtitleLabel;

        [Tooltip("Текст со статистикой спасённых жителей на завершённом уровне.")]
        [SerializeField] private TMP_Text rescuedLabel;

        [Tooltip("Текст со статистикой погибших жителей на завершённом уровне.")]
        [SerializeField] private TMP_Text failedLabel;

        [Tooltip("Текст с количеством жителей, оставшихся на уровне.")]
        [SerializeField] private TMP_Text remainingLabel;

        [Tooltip("Итоговое сообщение, зависящее от того, все ли жители были спасены.")]
        [SerializeField] private TMP_Text summaryLabel;

        [Tooltip("Текст на кнопке продолжения к следующему уровню.")]
        [SerializeField] private TMP_Text nextButtonLabel;

        private Action nextAction;
        private int rescuedCount;
        private int totalCount;
        private int failedCount;
        private int remainingCount;
        private bool showNextButton = true;
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

        public void SetNextButtonVisible(bool visible)
        {
            showNextButton = visible;

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(showNextButton);
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

            HideLegacyRestartButton();
        }

        private void UnbindButtons()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNext);
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

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(showNextButton);
            }

            HideLegacyRestartButton();
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
            nextButtonLabel ??= FindLabel("Panel/Buttons/NextButton/Label");
            HideLegacyRestartButton();
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

        private void HideLegacyRestartButton()
        {
            Transform restartButtonTransform = transform.Find(RestartButtonPath);
            if (restartButtonTransform != null)
            {
                restartButtonTransform.gameObject.SetActive(false);
            }
        }

        private void OnSwitchLanguage(string language)
        {
            RefreshLocalizedText();
        }
    }
}
