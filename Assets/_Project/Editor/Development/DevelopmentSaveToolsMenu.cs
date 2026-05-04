using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using YG;

namespace _Project.Editor.Development
{
    public static class DevelopmentSaveToolsMenu
    {
        private const string ClearSaveMenuPath = "Tools/Development/Очистить все сохранения";
        private const string LocalStorageSaveKey = "YG2_SavesData";
        private const string FirstSessionFlagKey = "WasFirstGameSession_YG";

        [MenuItem(ClearSaveMenuPath)]
        private static void ClearSaves()
        {
            if (!EditorUtility.DisplayDialog(
                    "Очистить сохранения",
                    "Будут удалены все сохранённые данные прогресса: уровни, оружие, патроны и project-side ownership.",
                    "Удалить",
                    "Отмена"))
            {
                return;
            }

            bool deletedAnything = false;
            bool failed = false;

            string editorSavesPath = Path.Combine(InfoYG.PATCH_PC_EDITOR, "SavesEditorYG2.json");
            if (File.Exists(editorSavesPath))
            {
                try
                {
                    File.Delete(editorSavesPath);
                    deletedAnything = true;
                }
                catch (IOException exception)
                {
                    failed = true;
                    Debug.LogWarning($"Не удалось удалить файл сохранений редактора '{editorSavesPath}'. {exception.Message}");
                }
                catch (UnauthorizedAccessException exception)
                {
                    failed = true;
                    Debug.LogWarning($"Нет доступа к удалению файла сохранений редактора '{editorSavesPath}'. {exception.Message}");
                }
            }

            if (PlayerPrefs.HasKey(LocalStorageSaveKey))
            {
                PlayerPrefs.DeleteKey(LocalStorageSaveKey);
                deletedAnything = true;
            }

            if (PlayerPrefs.HasKey(FirstSessionFlagKey))
            {
                PlayerPrefs.DeleteKey(FirstSessionFlagKey);
                deletedAnything = true;
            }

            PlayerPrefs.Save();

            YG2.saves = new SavesYG();
            AssetDatabase.Refresh();

            if (failed)
            {
                Debug.LogWarning("Удаление сохранений завершилось с ошибками. Подробности смотрите в сообщениях выше.");
                return;
            }

            if (deletedAnything)
            {
                Debug.Log("Все сохранения удалены: прогресс уровней, оружие, патроны и project-side ownership очищены.");
            }
            else
            {
                Debug.Log("Сохранения не найдены: удалять было нечего.");
            }
        }
    }
}
