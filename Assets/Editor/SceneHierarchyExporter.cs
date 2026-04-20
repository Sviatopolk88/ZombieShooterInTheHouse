using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Editor.SceneTools
{
    public static class SceneHierarchyExporter
    {
        private const string DefaultFileName = "SceneHierarchy.txt";

        [MenuItem("Tools/Scene/Export Hierarchy To TXT")]
        public static void ExportSceneHierarchy()
        {
            var scene = SceneManager.GetActiveScene();

            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("Сцена не загружена.");
                return;
            }

            string path = EditorUtility.SaveFilePanel(
                "Сохранить иерархию сцены",
                Application.dataPath,
                DefaultFileName,
                "txt"
            );

            if (string.IsNullOrEmpty(path))
                return;

            var sb = new StringBuilder();

            sb.AppendLine($"Scene: {scene.name}");
            sb.AppendLine("====================================");

            var rootObjects = scene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                WriteGameObject(root, sb, 0);
            }

            File.WriteAllText(path, sb.ToString());

            Debug.Log($"Иерархия сцены сохранена в:\n{path}");
        }

        private static void WriteGameObject(GameObject go, StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent * 2);

            sb.AppendLine($"{indentStr}- {go.name}");

            // Компоненты (можно убрать, если не нужны)
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null)
                {
                    sb.AppendLine($"{indentStr}  [Missing Component]");
                    continue;
                }

                // Пропускаем Transform, чтобы не засорять вывод
                if (comp is Transform) continue;

                sb.AppendLine($"{indentStr}  ({comp.GetType().Name})");
            }

            foreach (Transform child in go.transform)
            {
                WriteGameObject(child.gameObject, sb, indent + 1);
            }
        }
    }
}