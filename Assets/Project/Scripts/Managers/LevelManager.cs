using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using Project.Scripts.Character;
using Project.Scripts.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Project.Scripts.Managers
{
    public class LevelManager : NetworkSceneManagerBase
    {
        private const int ARENA_SCENE = 1;
        private const int LOBBY_SCENE = 0;

        [Inject]private UIManager m_UIManager;
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public  async void LoadArena()
        {
            await loadScene(ARENA_SCENE);
            
            if (GameLauncher.NetworkRunner.GameMode != GameMode.Host)
            {
                while (CharacterBase.SpawnedCharacters.Count <1 )
                {
                    await UniTask.Yield();
                }
            }
            
            RoomPlayer.Local.RPC_OnPlayerLoadLevel(GameLauncher.NetworkRunner.LocalPlayer);
        }

        private async UniTask loadScene(int index)
        {
            await SceneManager.LoadSceneAsync(index);
        }

        public void SpawnCounter()
        {
            GameLauncher.GameState = Enums.eGameState.Score;
            m_UIManager.ShowScoreScreen();
        }
        
        protected override IEnumerator SwitchScene(SceneRef prevScene, SceneRef newScene, FinishedLoadingDelegate finished)
        {
            List<NetworkObject> sceneObjects = new List<NetworkObject>();
        
            if (newScene == ARENA_SCENE)
            {
                yield return SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Single);
                Scene loadedScene = SceneManager.GetSceneByBuildIndex(newScene);
                Debug.Log($"Loaded scene {newScene}: {loadedScene}");
                sceneObjects = FindNetworkObjects(loadedScene, disable: false);
            }
        
            finished(sceneObjects);
        }
    }
}