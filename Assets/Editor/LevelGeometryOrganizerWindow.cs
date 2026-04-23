using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Editor.SceneTools
{
    public sealed class LevelGeometryOrganizerWindow : EditorWindow
    {
        private const string GeometryRootName = "Geometry";
        private const string MenuPath = "Tools/Scene/Organize Level Geometry";

        [SerializeField, Tooltip("Создавать папку Uncategorized для объектов без надёжной категории.")]
        private bool createUncategorizedFolder;

        [SerializeField, Tooltip("Показывать в превью список объектов, которые инструмент пропустит.")]
        private bool showSkippedObjects = true;

        private Vector2 scrollPosition;
        private OrganizationPlan currentPlan;

        [MenuItem(MenuPath)]
        public static void OpenWindow()
        {
            LevelGeometryOrganizerWindow window = GetWindow<LevelGeometryOrganizerWindow>();
            window.titleContent = new GUIContent("Geometry Organizer");
            window.minSize = new Vector2(520f, 420f);
            window.RefreshPlan();
        }

        private void OnEnable()
        {
            RefreshPlan();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Geometry Organizer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Инструмент работает только с root-объектом Geometry активной сцены. " +
                "Он создаёт организационные папки и переносит только явно классифицируемые прямые дочерние объекты Geometry. " +
                "Неоднозначные объекты и объекты со скриптами остаются на месте.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            createUncategorizedFolder = EditorGUILayout.Toggle(
                new GUIContent("Создать Uncategorized", "Создать папку Uncategorized и переносить в неё неклассифицированную геометрию без скриптов."),
                createUncategorizedFolder);
            showSkippedObjects = EditorGUILayout.Toggle(
                new GUIContent("Показывать пропуски", "Показывать в превью список объектов, которые были пропущены ради безопасности."),
                showSkippedObjects);

            if (EditorGUI.EndChangeCheck())
            {
                RefreshPlan();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Обновить Preview", GUILayout.Height(28f)))
                {
                    RefreshPlan();
                }

                using (new EditorGUI.DisabledScope(currentPlan == null || !currentPlan.CanApply))
                {
                    if (GUILayout.Button("Применить", GUILayout.Height(28f)))
                    {
                        ApplyPlan();
                    }
                }
            }

            EditorGUILayout.Space(8f);

            if (currentPlan == null)
            {
                EditorGUILayout.HelpBox("План ещё не построен.", MessageType.Warning);
                return;
            }

            MessageType summaryType = currentPlan.HasErrors
                ? MessageType.Error
                : currentPlan.CanApply ? MessageType.Info : MessageType.Warning;

            EditorGUILayout.HelpBox(currentPlan.Summary, summaryType);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.TextArea(currentPlan.Details, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void RefreshPlan()
        {
            currentPlan = OrganizationPlan.Build(SceneManager.GetActiveScene(), createUncategorizedFolder, showSkippedObjects);
            Repaint();
        }

        private void ApplyPlan()
        {
            if (currentPlan == null || !currentPlan.CanApply)
            {
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Организовать Geometry",
                    $"Будут перемещены {currentPlan.MoveCount} объектов внутри '{GeometryRootName}'.\n\n" +
                    "Инструмент затронет только прямых детей Geometry и создаст недостающие папки-контейнеры.\n" +
                    "Продолжить?",
                    "Применить",
                    "Отмена"))
            {
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Organize Level Geometry");

            try
            {
                Transform geometryRoot = currentPlan.GeometryRoot;
                Dictionary<string, Transform> folderMap = new Dictionary<string, Transform>(StringComparer.Ordinal);

                for (int i = 0; i < currentPlan.RequiredFolders.Count; i++)
                {
                    string folderName = currentPlan.RequiredFolders[i];
                    Transform folder = FindDirectChild(geometryRoot, folderName);
                    if (folder == null)
                    {
                        GameObject folderObject = new GameObject(folderName);
                        Undo.RegisterCreatedObjectUndo(folderObject, "Create Geometry Folder");
                        folder = folderObject.transform;
                        folder.SetParent(geometryRoot, false);
                        folder.localPosition = Vector3.zero;
                        folder.localRotation = Quaternion.identity;
                        folder.localScale = Vector3.one;
                    }

                    folderMap[folderName] = folder;
                }

                for (int i = 0; i < currentPlan.Moves.Count; i++)
                {
                    PlannedMove move = currentPlan.Moves[i];
                    if (move.ObjectTransform == null || !folderMap.TryGetValue(move.TargetFolderName, out Transform targetFolder))
                    {
                        continue;
                    }

                    if (move.ObjectTransform.parent == targetFolder)
                    {
                        continue;
                    }

                    Undo.SetTransformParent(move.ObjectTransform, targetFolder, "Move Geometry Object");
                    move.ObjectTransform.SetSiblingIndex(targetFolder.childCount - 1);
                    EditorUtility.SetDirty(move.ObjectTransform.gameObject);
                }

                EditorUtility.SetDirty(geometryRoot.gameObject);
                Undo.CollapseUndoOperations(undoGroup);
            }
            finally
            {
                RefreshPlan();
            }
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private sealed class OrganizationPlan
        {
            private static readonly string[] CategoryOrder =
            {
                "Shell",
                "Rooms",
                "Corridors",
                "Stairs",
                "Exterior",
                "Furniture",
                "SmallProps",
                "DecorativeSets",
                "Uncategorized"
            };

            private static readonly CategoryRule[] Rules =
            {
                new CategoryRule("Stairs", "stair", "stairs", "railing", "balustrade", "banister"),
                new CategoryRule("Exterior", "exterior", "facade", "roof", "awning", "outside", "street"),
                new CategoryRule("Corridors", "corridor", "hallway", "hall", "passage"),
                new CategoryRule("Rooms", "room", "kitchen", "office", "bath", "bedroom", "lobby", "toilet"),
                new CategoryRule("Furniture", "chair", "table", "desk", "cabinet", "sofa", "couch", "shelf", "locker", "bench", "bed", "wardrobe"),
                new CategoryRule("SmallProps", "prop", "box", "crate", "barrel", "trash", "paper", "bottle", "book", "monitor", "keyboard", "lamp", "plant"),
                new CategoryRule("DecorativeSets", "decor", "deco", "set", "cluster", "composition"),
                new CategoryRule("Shell", "wall", "floor", "ceiling", "pillar", "beam", "trim", "frame", "shell", "bld_")
            };

            public Transform GeometryRoot { get; private set; }
            public List<PlannedMove> Moves { get; } = new List<PlannedMove>();
            public List<string> RequiredFolders { get; } = new List<string>();
            public bool HasErrors { get; private set; }
            public bool CanApply => !HasErrors && GeometryRoot != null && Moves.Count > 0;
            public int MoveCount => Moves.Count;
            public string Summary { get; private set; } = string.Empty;
            public string Details { get; private set; } = string.Empty;

            public static OrganizationPlan Build(Scene scene, bool createUncategorizedFolder, bool showSkippedObjects)
            {
                OrganizationPlan plan = new OrganizationPlan();
                StringBuilder details = new StringBuilder(1024);

                if (!scene.IsValid() || !scene.isLoaded)
                {
                    plan.HasErrors = true;
                    plan.Summary = "Активная сцена не загружена.";
                    plan.Details = "Откройте сцену Level_1 или другую сцену, где существует root Geometry.";
                    return plan;
                }

                Transform geometryRoot = FindGeometryRoot(scene);
                if (geometryRoot == null)
                {
                    plan.HasErrors = true;
                    plan.Summary = $"Root-объект '{GeometryRootName}' не найден в активной сцене.";
                    plan.Details = "Инструмент работает только с точным scene root 'Geometry'.";
                    return plan;
                }

                plan.GeometryRoot = geometryRoot;

                Dictionary<string, int> categoryCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                List<string> skippedLines = new List<string>();

                for (int i = 0; i < geometryRoot.childCount; i++)
                {
                    Transform child = geometryRoot.GetChild(i);
                    if (IsOrganizerFolder(child.name))
                    {
                        continue;
                    }

                    string skipReason = GetSkipReason(child);
                    if (!string.IsNullOrEmpty(skipReason))
                    {
                        skippedLines.Add($"- {child.name}: {skipReason}");
                        continue;
                    }

                    if (!TryClassify(child.name, createUncategorizedFolder, out string categoryName, out string classificationReason))
                    {
                        skippedLines.Add($"- {child.name}: нет надёжного правила классификации");
                        continue;
                    }

                    plan.Moves.Add(new PlannedMove(child, categoryName, classificationReason));

                    if (!categoryCounts.ContainsKey(categoryName))
                    {
                        categoryCounts[categoryName] = 0;
                    }

                    categoryCounts[categoryName]++;
                }

                for (int i = 0; i < CategoryOrder.Length; i++)
                {
                    string categoryName = CategoryOrder[i];
                    if (!categoryCounts.ContainsKey(categoryName))
                    {
                        continue;
                    }

                    plan.RequiredFolders.Add(categoryName);
                }

                details.AppendLine($"Сцена: {scene.name}");
                details.AppendLine($"Geometry root: {GetHierarchyPath(geometryRoot)}");
                details.AppendLine($"К перемещению: {plan.Moves.Count}");
                details.AppendLine($"Пропущено: {skippedLines.Count}");
                details.AppendLine();

                if (plan.Moves.Count > 0)
                {
                    details.AppendLine("Категории:");
                    for (int i = 0; i < CategoryOrder.Length; i++)
                    {
                        string categoryName = CategoryOrder[i];
                        if (categoryCounts.TryGetValue(categoryName, out int count))
                        {
                            details.AppendLine($"- {categoryName}: {count}");
                        }
                    }

                    details.AppendLine();
                    details.AppendLine("План перемещений:");
                    for (int i = 0; i < plan.Moves.Count; i++)
                    {
                        PlannedMove move = plan.Moves[i];
                        details.AppendLine($"- {move.ObjectTransform.name} -> {move.TargetFolderName} ({move.Reason})");
                    }
                }

                if (showSkippedObjects && skippedLines.Count > 0)
                {
                    details.AppendLine();
                    details.AppendLine("Пропущенные объекты:");
                    for (int i = 0; i < skippedLines.Count; i++)
                    {
                        details.AppendLine(skippedLines[i]);
                    }
                }

                if (plan.Moves.Count == 0)
                {
                    plan.Summary = skippedLines.Count > 0
                        ? "Надёжно классифицируемых прямых детей Geometry не найдено. Инструмент ничего не изменит."
                        : "Geometry уже организован или подходящих объектов для переноса не найдено.";
                }
                else
                {
                    plan.Summary =
                        $"Будут созданы/использованы {plan.RequiredFolders.Count} папок и перемещены {plan.Moves.Count} прямых детей Geometry. " +
                        "Инструмент не трогает другие scene roots и пропускает объекты со скриптами.";
                }

                plan.Details = details.ToString();
                return plan;
            }

            private static Transform FindGeometryRoot(Scene scene)
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    if (string.Equals(roots[i].name, GeometryRootName, StringComparison.Ordinal))
                    {
                        return roots[i].transform;
                    }
                }

                return null;
            }

            private static bool TryClassify(string objectName, bool createUncategorizedFolder, out string categoryName, out string reason)
            {
                string normalizedName = NormalizeName(objectName);

                for (int i = 0; i < Rules.Length; i++)
                {
                    if (Rules[i].Matches(normalizedName))
                    {
                        categoryName = Rules[i].CategoryName;
                        reason = $"совпадение по имени: {Rules[i].MatchedToken}";
                        return true;
                    }
                }

                if (createUncategorizedFolder)
                {
                    categoryName = "Uncategorized";
                    reason = "не найдено явное правило, отправлено в Uncategorized";
                    return true;
                }

                categoryName = string.Empty;
                reason = string.Empty;
                return false;
            }

            private static string GetSkipReason(Transform transform)
            {
                Component[] components = transform.GetComponentsInChildren<Component>(true);
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (component == null)
                    {
                        return "обнаружен Missing Component";
                    }

                    if (component is Transform
                        || component is MeshRenderer
                        || component is SkinnedMeshRenderer
                        || component is MeshFilter
                        || component is Collider
                        || component is LODGroup
                        || component is OcclusionPortal
                        || component is ReflectionProbe
                        || component is LightProbeGroup
                        || component is Light
                        || component is Terrain
                        || component is TerrainCollider)
                    {
                        continue;
                    }

                    if (component is MonoBehaviour)
                    {
                        return $"обнаружен скрипт '{component.GetType().Name}'";
                    }
                }

                return string.Empty;
            }

            private static string NormalizeName(string objectName)
            {
                return string.IsNullOrWhiteSpace(objectName)
                    ? string.Empty
                    : objectName.Trim().ToLowerInvariant();
            }

            private static bool IsOrganizerFolder(string objectName)
            {
                for (int i = 0; i < CategoryOrder.Length; i++)
                {
                    if (string.Equals(objectName, CategoryOrder[i], StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static string GetHierarchyPath(Transform transform)
            {
                List<string> names = new List<string>();
                Transform current = transform;
                while (current != null)
                {
                    names.Add(current.name);
                    current = current.parent;
                }

                names.Reverse();
                return string.Join("/", names);
            }
        }

        private readonly struct PlannedMove
        {
            public PlannedMove(Transform objectTransform, string targetFolderName, string reason)
            {
                ObjectTransform = objectTransform;
                TargetFolderName = targetFolderName;
                Reason = reason;
            }

            public Transform ObjectTransform { get; }
            public string TargetFolderName { get; }
            public string Reason { get; }
        }

        private sealed class CategoryRule
        {
            private readonly string[] tokens;

            public CategoryRule(string categoryName, params string[] tokens)
            {
                CategoryName = categoryName;
                this.tokens = tokens ?? Array.Empty<string>();
            }

            public string CategoryName { get; }
            public string MatchedToken { get; private set; } = string.Empty;

            public bool Matches(string normalizedObjectName)
            {
                MatchedToken = string.Empty;

                if (string.IsNullOrEmpty(normalizedObjectName))
                {
                    return false;
                }

                for (int i = 0; i < tokens.Length; i++)
                {
                    string token = tokens[i];
                    if (!string.IsNullOrEmpty(token) && normalizedObjectName.Contains(token))
                    {
                        MatchedToken = token;
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
