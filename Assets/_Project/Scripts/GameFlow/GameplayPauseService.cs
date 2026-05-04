using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.GameFlow
{
    /// <summary>
    /// Project-side сервис модальной паузы gameplay.
    /// Ставит игру на паузу только для своих владельцев и не снимает чужую паузу, если игра уже была остановлена ранее.
    /// </summary>
    public static class GameplayPauseService
    {
        private static readonly HashSet<object> Owners = new();

        private static bool pauseAppliedByService;
        private static float previousTimeScale = 1f;

        public static bool IsPausedBy(object owner)
        {
            return owner != null && Owners.Contains(owner);
        }

        public static void Pause(object owner)
        {
            if (owner == null || !Owners.Add(owner))
            {
                return;
            }

            if (Owners.Count > 1)
            {
                return;
            }

            previousTimeScale = Time.timeScale;
            pauseAppliedByService = Time.timeScale > 0f;

            if (pauseAppliedByService)
            {
                Time.timeScale = 0f;
            }
        }

        public static void Resume(object owner)
        {
            if (owner == null || !Owners.Remove(owner))
            {
                return;
            }

            if (Owners.Count > 0)
            {
                return;
            }

            if (pauseAppliedByService)
            {
                Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            }

            pauseAppliedByService = false;
            previousTimeScale = 1f;
        }
    }
}
