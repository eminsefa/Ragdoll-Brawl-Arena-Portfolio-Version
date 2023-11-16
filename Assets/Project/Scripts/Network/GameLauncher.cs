using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using Project.Scripts.Character;
using Project.Scripts.Managers;
using Project.Scripts.UI;
using UnityEngine;
using Zenject;

namespace Project.Scripts.Network
{
    public class GameLauncher : MonoBehaviour
    {
        public static NetworkRunner NetworkRunner;

        public static Enums.eGameState GameState = Enums.eGameState.Lobby;

        [Inject] private GameManager      m_GameManager;
        [Inject] private LevelManager     m_LevelManager;
        [Inject] private NetworkRunner    m_NetworkRunner;
        [Inject] private ResourcesManager m_ResourcesManager;
        
        public static Dictionary<PlayerRef, RoomPlayer> SpawnedRoomPlayers = new Dictionary<PlayerRef, RoomPlayer>();

        private async void Start()
        {
            while (!m_ResourcesManager.AreResourcesSet)
            {
                await UniTask.Yield();
            }
            NetworkRunner = m_NetworkRunner;

            m_NetworkRunner.ProvideInput = true;
            var task=m_NetworkRunner.StartGame(new StartGameArgs
                                      {
                                          SceneManager = m_LevelManager,
                                          Scene        = 0,
                                          SessionName  = "PortfolioVersion",
                                          GameMode     = GameMode.AutoHostOrClient
                                      });

            DontDestroyOnLoad(gameObject);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef playerRef)
        {
            if (m_NetworkRunner.GameMode != GameMode.Host) return;

            Spawn(playerRef);

            if (playerRef != m_NetworkRunner.LocalPlayer)
            {
                return;
            }

            var gravityPrefab = m_ResourcesManager.GravityPrefab;
            m_NetworkRunner.Spawn(gravityPrefab);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (m_NetworkRunner.GameMode != GameMode.Host) return;

            if (SpawnedRoomPlayers.TryGetValue(player, out RoomPlayer roomplayer))
            {
                if (roomplayer.Controller.TryGetComponent(out NetworkObject obj)) runner.Despawn(obj);
                if (roomplayer.TryGetComponent(out NetworkObject objMain)) runner.Despawn(objMain);
                SpawnedRoomPlayers.Remove(player);
            }

            if (GameState == Enums.eGameState.Arena) m_GameManager.OnPlayerLeft();
        }

        public void OnServerShutDown(NetworkRunner runner, ShutdownReason reason)
        {
            BotController.PlayerScore = 0;
        }

        private void Spawn(PlayerRef playerRef)
        {
            var        roomPlayer          = m_ResourcesManager.RoomPlayerPrefab;
            RoomPlayer networkPlayerObject = m_NetworkRunner.Spawn(roomPlayer, Vector3.zero, Quaternion.identity, playerRef);

            if (SpawnedRoomPlayers.ContainsKey(playerRef)) SpawnedRoomPlayers[playerRef] = networkPlayerObject;
            else SpawnedRoomPlayers.Add(playerRef, networkPlayerObject);
        }
    }
}