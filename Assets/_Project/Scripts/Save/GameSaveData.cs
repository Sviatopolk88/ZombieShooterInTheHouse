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

        public GameSaveData Clone()
        {
            return new GameSaveData
            {
                version = version,
                currentLevel = currentLevel,
                weapons = weapons != null ? (string[])weapons.Clone() : Array.Empty<string>(),
                weaponMagazines = CloneWeaponMagazines(weaponMagazines),
                ammo9mm = ammo9mm,
                ammo12Gauge = ammo12Gauge
            };
        }

        private static WeaponMagazineSaveData[] CloneWeaponMagazines(WeaponMagazineSaveData[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<WeaponMagazineSaveData>();
            }

            WeaponMagazineSaveData[] copy = new WeaponMagazineSaveData[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                WeaponMagazineSaveData entry = source[i];
                copy[i] = entry == null
                    ? null
                    : new WeaponMagazineSaveData
                    {
                        weaponId = entry.weaponId,
                        magazine = entry.magazine
                    };
            }

            return copy;
        }
    }

    [Serializable]
    public sealed class WeaponMagazineSaveData
    {
        public string weaponId;
        public int magazine;
    }
}
