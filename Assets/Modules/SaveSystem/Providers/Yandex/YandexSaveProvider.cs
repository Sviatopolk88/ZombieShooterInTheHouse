using System.Collections.Generic;
using YG;

namespace Modules.SaveSystem
{
    /// <summary>
    /// Reusable provider сохранений поверх PluginYG2 / YG2.
    /// Хранит произвольные JSON-строки в generic key/value storage внутри YG2.saves.
    /// </summary>
    public sealed class YandexSaveProvider : ISaveProvider
    {
        private bool initialized;

        public bool IsReady => initialized && YG2.isSDKEnabled && YG2.saves != null;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            EnsureStorage();
        }

        public void Save(string key, string json)
        {
            EnsureStorage();

            List<string> keys = YG2.saves.saveSystemKeys;
            List<string> values = YG2.saves.saveSystemValues;
            int index = keys.IndexOf(key);

            if (index >= 0)
            {
                values[index] = json;
            }
            else
            {
                keys.Add(key);
                values.Add(json);
            }

            YG2.SaveProgress();
        }

        public string Load(string key)
        {
            EnsureStorage();

            List<string> keys = YG2.saves.saveSystemKeys;
            int index = keys.IndexOf(key);
            if (index < 0 || index >= YG2.saves.saveSystemValues.Count)
            {
                return string.Empty;
            }

            return YG2.saves.saveSystemValues[index];
        }

        public bool HasKey(string key)
        {
            EnsureStorage();
            return YG2.saves.saveSystemKeys.Contains(key);
        }

        private static void EnsureStorage()
        {
            if (YG2.saves == null)
            {
                YG2.saves = new SavesYG();
            }

            YG2.saves.saveSystemKeys ??= new List<string>();
            YG2.saves.saveSystemValues ??= new List<string>();
        }
    }
}
