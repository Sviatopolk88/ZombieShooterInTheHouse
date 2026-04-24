using System.Collections.Generic;
using NeoFPS;
using UnityEngine;

namespace _Project.Scripts.Pickups
{
    [DisallowMultipleComponent]
    public sealed class WeaponPickupAutoTrigger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Ссылка на штатный NeoFPS weapon pickup, через который оружие добавляется в инвентарь игрока.")]
        private InteractivePickup weaponPickup;

        private readonly HashSet<Transform> overlappingCharacters = new();

        private void Reset()
        {
            weaponPickup = GetComponent<InteractivePickup>();
        }

        private void OnValidate()
        {
            if (weaponPickup == null)
            {
                weaponPickup = GetComponent<InteractivePickup>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryPickup(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryPickup(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (TryResolveCharacter(other, out _, out Transform characterRoot))
            {
                overlappingCharacters.Remove(characterRoot);
            }
        }

        private void OnDisable()
        {
            overlappingCharacters.Clear();
        }

        private void TryPickup(Collider other)
        {
            if (weaponPickup == null)
            {
                return;
            }

            if (!TryResolveCharacter(other, out ICharacter character, out Transform characterRoot))
            {
                return;
            }

            if (!overlappingCharacters.Add(characterRoot))
            {
                return;
            }

            weaponPickup.Interact(character);
        }

        private static bool TryResolveCharacter(Collider other, out ICharacter character, out Transform characterRoot)
        {
            character = null;
            characterRoot = null;

            if (other == null)
            {
                return false;
            }

            character = other.GetComponentInParent<ICharacter>();
            if (character is not Component characterComponent)
            {
                return false;
            }

            characterRoot = characterComponent.transform;
            return characterRoot != null;
        }
    }
}
