using System.Collections;
using Modules.AdsCore;
using Modules.PurchasesCore;
using Modules.SceneLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Systems.SceneFlow
{
    public sealed class BootstrapSceneStartup : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string mainSceneName = ProjectSceneNames.Main;
        [SerializeField] private string levelSceneName = ProjectSceneNames.FirstLevel;
        [SerializeField] private bool unloadBootstrapSceneAfterLoad = true;

        private void Start()
        {
            PurchaseService.Instance.Warmup();
            StartCoroutine(LoadInitialScenes());
        }

        private IEnumerator LoadInitialScenes()
        {
            // Bootstrap сцена нужна только для стартовой инициализации и сборки игрового набора сцен.
            // Main загружается additively и остается постоянной сценой с игроком, камерой и UI.
            // Level тоже грузится additively поверх Main, чтобы уровень можно было менять отдельно.

            // Используем SceneManager напрямую, потому что SceneLoader не работает с AsyncOperation.
            AsyncOperation loadMainOperation = SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Additive);
            yield return loadMainOperation;

            // Используем SceneManager напрямую, потому что SceneLoader не работает с AsyncOperation.
            AsyncOperation loadLevelOperation = SceneManager.LoadSceneAsync(levelSceneName, LoadSceneMode.Additive);
            yield return loadLevelOperation;

            SceneLoader.SetActiveScene(mainSceneName);
            AdsService.Instance.TryShowInterstitial(AdsInterstitialPlacement.GameStart);

            if (unloadBootstrapSceneAfterLoad)
            {
                SceneLoader.UnloadScene(gameObject.scene.name);
            }
        }
    }
}
