using System;
using UnityEngine;

namespace Modules.SaveSystem
{
    /// <summary>
    /// Reusable фасад системы сохранений.
    /// Не знает про gameplay и работает только с сериализуемыми DTO.
    /// </summary>
    public sealed class SaveService
    {
        private static SaveService instance;

        private readonly SaveProjectSettings settings = new();
        private readonly ISaveProvider provider;

        private SaveService()
        {
            provider = CreateProvider(settings.ProviderType);
            provider.Initialize();
        }

        public static SaveService Instance => instance ??= new SaveService();

        public bool IsReady => provider.IsReady;

        public void Warmup()
        {
            // Явная точка ранней инициализации из bootstrap.
        }

        public bool HasKey(string key)
        {
            if (!provider.IsReady || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return provider.HasKey(key);
        }

        public bool SaveRaw(string key, string json)
        {
            if (!provider.IsReady)
            {
                Debug.LogWarning("SaveService: provider ещё не готов к сохранению.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning("SaveService: ключ сохранения пустой.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"SaveService: JSON для ключа '{key}' пустой.");
                return false;
            }

            provider.Save(key, json);
            return true;
        }

        public bool TryLoadRaw(string key, out string json)
        {
            json = string.Empty;

            if (!provider.IsReady)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(key) || !provider.HasKey(key))
            {
                return false;
            }

            json = provider.Load(key);
            return !string.IsNullOrWhiteSpace(json);
        }

        public bool Save<T>(string key, T data)
        {
            if (data == null)
            {
                Debug.LogWarning($"SaveService: данные для ключа '{key}' отсутствуют.");
                return false;
            }

            string json = JsonUtility.ToJson(data);
            return SaveRaw(key, json);
        }

        public bool TryLoad<T>(string key, out T data) where T : class
        {
            data = null;

            if (!TryLoadRaw(key, out string json))
            {
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<T>(json);
                return data != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SaveService: не удалось десериализовать ключ '{key}'. {exception.Message}");
                data = null;
                return false;
            }
        }

        private static ISaveProvider CreateProvider(SaveProviderType providerType)
        {
            return providerType switch
            {
                SaveProviderType.YandexGames => new YandexSaveProvider(),
                _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, "Неизвестный save provider.")
            };
        }
    }
}
