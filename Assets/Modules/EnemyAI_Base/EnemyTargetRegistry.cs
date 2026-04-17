using System.Collections.Generic;
using UnityEngine;

namespace Modules.EnemyAI_Base
{
    /// <summary>
    /// Лёгкий реестр доступных целей для EnemyAI_Base.
    /// Позволяет снизить зависимость AI от полного поиска по сцене через теги.
    /// </summary>
    public static class EnemyTargetRegistry
    {
        private static readonly Dictionary<string, HashSet<Transform>> TargetsByTag = new();

        public static void Register(string tag, Transform target)
        {
            if (string.IsNullOrWhiteSpace(tag) || target == null)
            {
                return;
            }

            if (!TargetsByTag.TryGetValue(tag, out HashSet<Transform> targets))
            {
                targets = new HashSet<Transform>();
                TargetsByTag.Add(tag, targets);
            }

            targets.Add(target);
        }

        public static void Unregister(string tag, Transform target)
        {
            if (string.IsNullOrWhiteSpace(tag) || target == null)
            {
                return;
            }

            if (!TargetsByTag.TryGetValue(tag, out HashSet<Transform> targets))
            {
                return;
            }

            targets.Remove(target);

            if (targets.Count == 0)
            {
                TargetsByTag.Remove(tag);
            }
        }

        public static int CopyTargets(string tag, List<Transform> results)
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();

            if (string.IsNullOrWhiteSpace(tag))
            {
                return 0;
            }

            if (!TargetsByTag.TryGetValue(tag, out HashSet<Transform> targets))
            {
                return 0;
            }

            foreach (Transform target in targets)
            {
                if (target != null)
                {
                    results.Add(target);
                }
            }

            return results.Count;
        }
    }
}
