using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusion;
using Project.Scripts.Character;
using Project.Scripts.Enums;
using Project.Scripts.Network;
using Project.Scripts.Objects;
using Project.Scripts.UI;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Project.Scripts.Managers
{
    public class GameManager : MonoBehaviour
    {
        private const int BOT_PLAYER_REF_INDEX = -1;

        public static event Action OnGameEnded;

        private CancellationTokenSource m_BoostSpawnTaskCts;

        private LevelManager     m_LevelManager;
        private ResourcesManager m_ResourcesManager;

        [SerializeField] private Transform[] m_SpawnPoints;
        [SerializeField] private LayerMask   m_BodyLayer;

        [Inject] //Maybe try custom network factory injection
        public void InjectDependencies(ResourcesManager i_ResourcesManager, LevelManager i_LevelManager, UIManager i_UIManager, AudioManager i_AudioManager, PoolManager i_PoolManager)
        {
            m_ResourcesManager = i_ResourcesManager;
            m_LevelManager     = i_LevelManager;
            CharacterBase.InjectDependencies(i_UIManager, i_AudioManager);
            BodyPart.InjectDependencies(i_AudioManager, i_PoolManager);
        }

        private void OnEnable()
        {
            RoomPlayer.OnPlayerLoadLevel                        += OnPlayerLoadLevel;
            RoomPlayer.OnRoomPlayerCreated                      += OnRoomPlayerCreated;
            CharacterBase.OnCharacterDead                       += OnCharacterDead;
            BoostBase.OnBoostCollected                          += OnBoostCollected;
            GameStartCooldownTimer.OnGameStartCooldownCompleted += OnGameStartCooldownCompleted;

            DontDestroyOnLoad(gameObject);
        }

        private void OnDisable()
        {
            RoomPlayer.OnPlayerLoadLevel                        -= OnPlayerLoadLevel;
            RoomPlayer.OnRoomPlayerCreated                      -= OnRoomPlayerCreated;
            CharacterBase.OnCharacterDead                       -= OnCharacterDead;
            BoostBase.OnBoostCollected                          -= OnBoostCollected;
            GameStartCooldownTimer.OnGameStartCooldownCompleted -= OnGameStartCooldownCompleted;

            DOTween.KillAll();
        }

        private void SpawnEnvironment()
        {
            var env = m_ResourcesManager.EnvironmentPrefab;
            Instantiate(env);
        }

        private void OnPlayerLoadLevel(PlayerRef playerRef)
        {
            SpawnEnvironment();
            SpawnPlayerCharacter(playerRef);
        }

        private void OnRoomPlayerCreated(RoomPlayer roomPlayer)
        {
            roomPlayer.InjectDependency(this, m_LevelManager);
        }

        private void OnCharacterDead(CharacterBase character)
        {
            if (GameLauncher.NetworkRunner.GameMode != GameMode.Host) return;
            var aliveCharacters = CharacterBase.SpawnedCharacters.Where(x => x.IsActive).ToArray();

            if (aliveCharacters.Length > 1) return;

            //need a dummy roomplayer for bot later
            var winner = RoomPlayer.Players.FirstOrDefault(x => x.Controller.IsActive);
            if (winner != null) winner.AddScore();
            else BotController.Instance.AddScore();

            Invoke(nameof(CallGameEnded), 2f);
        }

        private void OnBoostCollected()
        {
            var uniTask = SpawnBoost();
        }

        private void OnGameStartCooldownCompleted()
        {
            if (GameLauncher.NetworkRunner.GameMode == GameMode.Host) ResetSpawn();
            GameLauncher.GameState = Enums.eGameState.Arena;
            // AudioManager.Instance.PlayFrontAudio(Enums.Enums.eAudioType.GameStart);
        }

        public void OnPlayerLeft()
        {
            CheckForBot(false);
        }

        private async void ResetSpawn()
        {
            var roomPlayers = RoomPlayer.Players;

            var controllers = CharacterBase.SpawnedCharacters;
            foreach (var controller in controllers)
            {
                var spawnPos = m_SpawnPoints[controllers.IndexOf(controller) % m_SpawnPoints.Length].position;
                controller.RPC_Init(spawnPos);
            }

            foreach (var controller in controllers)
            {
                while (!controller.IsInit)
                {
                    await UniTask.Yield();
                }
            }
            // for (var i = 0; i < roomPlayers.Count; i++)
            // {
            // var spawnPos   = m_SpawnPoints[CharacterBase.SpawnedCharacters.IndexOf(roomPlayers[i].Controller) % m_SpawnPoints.Length].position;
            // var controller = roomPlayers[i].Controller;
            // controller.RPC_Init(spawnPos);
            // }

            // foreach (var roomPlayer in roomPlayers)
            // {
            // while (!roomPlayer.Controller.IsInit)
            // {
            // await UniTask.Yield();
            // }
            // }

            foreach (var roomPlayer in roomPlayers)
            {
                roomPlayer.RPC_PlayerEnteredArena(roomPlayer.Controller);
            }

            if (BotController.Instance != null) BotController.Instance.ActivateController();

            // await UniTask.NextFrame();
            // CheckForBot();

            var uniTask = SpawnBoost();
        }

        private void SpawnPlayerCharacter(PlayerRef playerRef)
        {
            if (GameLauncher.NetworkRunner.GameMode != GameMode.Host) return;
            FirstSpawn(playerRef);

            if (playerRef != GameLauncher.NetworkRunner.LocalPlayer) return;
            var unitask = SpawnBoost();
        }

        private async UniTask UpdateArena()
        {
            foreach (var roomPlayer in RoomPlayer.Players)
            {
                if (roomPlayer.Controller != null && roomPlayer.Controller.IsActive) roomPlayer.Controller.UpdateCharacterValues();
            }

            var boost = BoostBase.Instance;
            if (boost != null)
            {
                boost.RPC_UpdatePosition(boost.IsActive, boost.transform.position);
            }

            await UniTask.NextFrame();
        }

        private async void FirstSpawn(PlayerRef playerRef)
        {
            var spawnPosition =m_SpawnPoints[RoomPlayer.Players.IndexOf(GameLauncher.SpawnedRoomPlayers[playerRef]) % m_SpawnPoints.Length].position;

            if (GameLauncher.GameState == eGameState.Arena)
            {
                m_BoostSpawnTaskCts = new CancellationTokenSource();
                spawnPosition = await FindEmptySpace(m_BoostSpawnTaskCts);    
            }

            var           player            = m_ResourcesManager.PlayerPrefab;
            NetworkObject networkController = GameLauncher.NetworkRunner.Spawn(player, spawnPosition, Quaternion.identity, playerRef);
            networkController.RequestStateAuthority();

            var roomPlayer = GameLauncher.SpawnedRoomPlayers[playerRef];

            if (networkController.TryGetComponent(out PlayerController controller))
            {
                while (!controller.IsSpawned)
                {
                    await UniTask.Yield();
                }

                controller.RPC_ResetController(true);
                controller.RPC_SetFaceID(roomPlayer.FaceId, false);
                while (!controller.IsReset)
                {
                    await UniTask.Yield();
                }

                controller.RPC_Init(spawnPosition);
            }

            if (playerRef != GameLauncher.NetworkRunner.LocalPlayer)
            {
                var updateTask = UpdateArena();
                while (updateTask.Status != UniTaskStatus.Succeeded)
                {
                    await UniTask.Yield();
                }
            }

            while (!controller.IsInit)
            {
                await UniTask.Yield();
            }

            roomPlayer.RPC_PlayerEnteredArena(controller);
            while (!controller.IsActive)
            {
                await UniTask.Yield();
            }

            // await UniTask.DelayFrame(4);
            CheckForBot();
        }

        private async void CallGameEnded()
        {
            RoomPlayer.Local.RPC_GameEnded();
            
            m_BoostSpawnTaskCts.Cancel(false);
            m_BoostSpawnTaskCts.Dispose();

            if (BoostBase.Instance != null) BoostBase.Instance.RPC_DeactivateSelf();
            
            await UniTask.Delay(1000);
            
            foreach (var spawnedCharacter in CharacterBase.SpawnedCharacters)
            {
                spawnedCharacter.RPC_ResetController();
            }
            
            var rng = new System.Random();
            m_SpawnPoints = m_SpawnPoints.OrderBy(x => rng.Next()).ToArray();
        }

        public void GameEnded()
        {
            DOTween.KillAll();
            OnGameEnded?.Invoke();
        }

        private async void CheckForBot(bool firstSpawnCheck = true)
        {
            if (GameLauncher.NetworkRunner.GameMode != GameMode.Host) return;

            var botController = BotController.Instance;
            if (RoomPlayer.Players.Count < 2)
            {
                m_BoostSpawnTaskCts = new CancellationTokenSource();
                var spawnPosition = await FindEmptySpace(m_BoostSpawnTaskCts);
                if (botController == null)
                {
                    var botPrefab = m_ResourcesManager.BotPrefab;
                    var botObj    = GameLauncher.NetworkRunner.Spawn(botPrefab, spawnPosition, Quaternion.identity);

                    botController = botObj.GetComponent<BotController>();
                    botController.RPC_ResetController(true);
                    while (!botController.IsReset)
                    {
                        await UniTask.Yield();
                    }
                }

                if (!firstSpawnCheck)
                {
                    botController.RPC_ResetController(true);
                    while (!botController.IsReset)
                    {
                        await UniTask.Yield();
                    }
                }

                botController.RPC_Init(spawnPosition);
                while (!botController.IsInit)
                {
                    await UniTask.Yield();
                }

                botController.ActivateController();
            }
            else
            {
                if (botController != null)
                {
                    botController.TryGetComponent(out NetworkObject obj);
                    GameLauncher.NetworkRunner.Despawn(obj);
                }
            }
        }

        private async UniTask SpawnBoost()
        {
            try
            {
                m_BoostSpawnTaskCts = new CancellationTokenSource();
                await UniTask.Delay((int) (Random.Range(5f, 6f) * 1000), cancellationToken: m_BoostSpawnTaskCts.Token);

                var spawnPos = await FindEmptySpace(m_BoostSpawnTaskCts);
                if (m_BoostSpawnTaskCts.IsCancellationRequested) return;

                if (BoostBase.Instance == null)
                {
                    var boost = m_ResourcesManager.BoostPrefab;
                    var o     = GameLauncher.NetworkRunner.Spawn(boost, spawnPos);
                    while (!o.IsValid)
                    {
                        await UniTask.Yield();
                    }
                }

                BoostBase.Instance.RPC_Init(spawnPos);
            }
            catch (Exception e)
            {
                if (!e.IsOperationCanceledException()) Debug.LogError($"error on spawn boost,:{e}");
                throw;
            }
        }

        private async UniTask<Vector3> FindEmptySpace(CancellationTokenSource i_Cts)
        {
            try
            {
                bool         isEmpty    = false;
                Collider2D[] cols       = new Collider2D[64];
                int          spawnIndex = 0;
                while (!isEmpty)
                {
                    spawnIndex = Random.Range(0, m_SpawnPoints.Length);
                    var pos   = (Vector2) m_SpawnPoints[spawnIndex].position;
                    var count = Physics2D.OverlapAreaNonAlloc(pos - Vector2.one * 3, pos + Vector2.one * 3, cols, m_BodyLayer);
                    isEmpty = count < 1;

                    if (i_Cts.IsCancellationRequested) return m_SpawnPoints[spawnIndex].position;

                    await UniTask.DelayFrame(2);
                }

                return m_SpawnPoints[spawnIndex].position;
            }
            catch (Exception e)
            {
                if (!e.IsOperationCanceledException()) Debug.LogError($"error on find empty space,:{e}");
                throw;
            }
        }
    }
}