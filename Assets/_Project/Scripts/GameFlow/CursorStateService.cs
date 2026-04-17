using NeoFPS;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.GameFlow
{
    public sealed class CursorStateService : MonoBehaviour
    {
        public enum CursorMode
        {
            Gameplay,
            UI
        }

        public static CursorStateService Instance { get; private set; }

        [SerializeField] private CursorMode initialMode = CursorMode.Gameplay;
        [SerializeField] private float playerResolveRetryInterval = 0.5f;

        private BaseCharacter playerCharacter;
        private CursorMode currentMode;
        private bool gameplayReturnBlocked;
        private bool hasStoredPlayerInputState;
        private bool storedAllowMoveInput;
        private bool storedAllowLookInput;
        private bool storedAllowWeaponInput;
        private float nextPlayerResolveTime;

        public CursorMode CurrentMode => currentMode;

        public bool IsGameplayMode => currentMode == CursorMode.Gameplay;

        public bool IsUiMode => currentMode == CursorMode.UI;

        public bool IsGameplayReturnBlocked => gameplayReturnBlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("CursorStateService: duplicate instance detected, destroying component.", this);
                Destroy(this);
                return;
            }

            Instance = this;
            currentMode = initialMode;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void Start()
        {
            ResolvePlayerCharacter();
            ApplyState();
        }

        private void Update()
        {
            if (!ShouldRetryResolvePlayer())
            {
                return;
            }

            nextPlayerResolveTime = Time.unscaledTime + Mathf.Max(0.1f, playerResolveRetryInterval);
            ApplyState();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetGameplayMode()
        {
            if (gameplayReturnBlocked)
            {
                return;
            }

            SetMode(CursorMode.Gameplay);
        }

        public void SetUiMode()
        {
            SetMode(CursorMode.UI);
        }

        public bool ToggleMode()
        {
            if (gameplayReturnBlocked)
            {
                return false;
            }

            SetMode(currentMode == CursorMode.Gameplay ? CursorMode.UI : CursorMode.Gameplay);
            return true;
        }

        public void EnterGameOverMode()
        {
            gameplayReturnBlocked = true;
            SetMode(CursorMode.UI);
        }

        public void ExitGameOverMode()
        {
            gameplayReturnBlocked = false;
            SetMode(CursorMode.Gameplay);
        }

        private void SetMode(CursorMode mode)
        {
            currentMode = mode;
            ApplyState();
        }

        private void ApplyState()
        {
            ResolvePlayerCharacter();
            ApplyCursorState();
            ApplyPlayerInputState();
        }

        private void ApplyCursorState()
        {
            NeoFpsInputManagerBase.captureMouseCursor = currentMode == CursorMode.Gameplay;
        }

        private void ApplyPlayerInputState()
        {
            if (playerCharacter == null)
            {
                hasStoredPlayerInputState = false;
                return;
            }

            if (currentMode == CursorMode.UI)
            {
                StorePlayerInputStateIfRequired();
                playerCharacter.allowMoveInput = false;
                playerCharacter.allowLookInput = false;
                playerCharacter.allowWeaponInput = false;
                return;
            }

            if (!hasStoredPlayerInputState)
            {
                return;
            }

            playerCharacter.allowMoveInput = storedAllowMoveInput;
            playerCharacter.allowLookInput = storedAllowLookInput;
            playerCharacter.allowWeaponInput = storedAllowWeaponInput;
            hasStoredPlayerInputState = false;
        }

        private void StorePlayerInputStateIfRequired()
        {
            if (hasStoredPlayerInputState || playerCharacter == null)
            {
                return;
            }

            // Запоминаем исходные флаги, чтобы после UI вернуть персонажа в прежнее состояние.
            storedAllowMoveInput = playerCharacter.allowMoveInput;
            storedAllowLookInput = playerCharacter.allowLookInput;
            storedAllowWeaponInput = playerCharacter.allowWeaponInput;
            hasStoredPlayerInputState = true;
        }

        private void ResolvePlayerCharacter()
        {
            BaseCharacter resolvedCharacter = null;

            // Main живёт отдельно от Level, поэтому игрока ищем динамически после загрузки сцены.
            BaseCharacter[] characters = FindObjectsByType<BaseCharacter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] != null && characters[i].isPlayerControlled)
                {
                    resolvedCharacter = characters[i];
                    break;
                }
            }

            if (playerCharacter == resolvedCharacter)
            {
                return;
            }

            playerCharacter = resolvedCharacter;
            hasStoredPlayerInputState = false;
        }

        private bool ShouldRetryResolvePlayer()
        {
            if (playerCharacter != null)
            {
                return false;
            }

            return Time.unscaledTime >= nextPlayerResolveTime;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            nextPlayerResolveTime = 0f;
            ApplyState();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            ResolvePlayerCharacter();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                return;
            }

            // После возврата фокуса повторно применяем актуальный режим курсора.
            ApplyState();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                return;
            }

            ApplyState();
        }
    }
}
