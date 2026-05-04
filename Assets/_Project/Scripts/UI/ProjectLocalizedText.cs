using _Project.Scripts.Localization;
using TMPro;
using UnityEngine;
using YG;

namespace _Project.Scripts.UI
{
    [DisallowMultipleComponent]
    public sealed class ProjectLocalizedText : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Текстовый компонент, в который выводится локализованная строка. Если поле пустое, компонент будет найден на этом объекте.")]
        private TMP_Text textTarget;

        [SerializeField]
        [Tooltip("Ключ строки из проектной таблицы локализации.")]
        private ProjectTextKey textKey = ProjectTextKey.BootstrapLoading;

        private void Reset()
        {
            textTarget = GetComponent<TMP_Text>();
            RefreshText();
        }

        private void OnValidate()
        {
            if (textTarget == null)
            {
                textTarget = GetComponent<TMP_Text>();
            }

            RefreshText();
        }

        private void OnEnable()
        {
            YG2.onSwitchLang += OnSwitchLanguage;
            RefreshText();
        }

        private void OnDisable()
        {
            YG2.onSwitchLang -= OnSwitchLanguage;
        }

        private void RefreshText()
        {
            if (textTarget != null)
            {
                textTarget.text = ProjectLocalizationYG.Get(textKey);
            }
        }

        private void OnSwitchLanguage(string language)
        {
            RefreshText();
        }
    }
}
