using System;

namespace _Project.Scripts.Save
{
    /// <summary>
    /// Простая DTO-модель прогресса проекта.
    /// Не содержит ссылок на MonoBehaviour и gameplay-компоненты.
    /// </summary>
    [Serializable]
    public sealed class GameSaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int currentLevel = 1;
        public string[] weapons = Array.Empty<string>();
        public WeaponMagazineSaveData[] weaponMagazines = Array.Empty<WeaponMagazineSaveData>();
        public int ammo9mm;
        public int ammo12Gauge;
    }

    [Serializable]
    public sealed class WeaponMagazineSaveData
    {
        public string weaponId;
        public int magazine;
    }
}
