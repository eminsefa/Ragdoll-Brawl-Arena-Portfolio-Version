using System;
using System.Collections.Generic;
using Fusion;
using Project.Scripts.Character;
using Project.Scripts.Managers;

namespace Project.Scripts.Network
{
    public class RoomPlayer : NetworkBehaviour
    {
        public static event Action<RoomPlayer> OnRoomPlayerCreated;
        public static event Action             OnFaceIdSelected;
        public static event Action<PlayerRef>  OnPlayerLoadLevel;
        public static event Action<PlayerRef>  OnPlayerEnterArena;

        public static List<RoomPlayer> Players = new List<RoomPlayer>();
        public static RoomPlayer       Local;

        private static GameManager  s_GameManager;
        private static LevelManager s_LevelManager;

        [Networked] public int              PlayerScore { get; private set; }
        public             PlayerController Controller  { get; private set; }
        [Networked] public int              FaceId      { get; private set; }

        public override void Spawned()
        {
            base.Spawned();

            Players.Add(this);

            if (Object.HasInputAuthority)
            {
                Local = this;

                var id = ClientInfo.FaceId;
                foreach (var roomPlayer in Players)
                {
                    if (roomPlayer.FaceId == id) id = -1;
                }

                RPC_SetFaceId(id);
                OnRoomPlayerCreated?.Invoke(this);
            }

            DontDestroyOnLoad(gameObject);
        }

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.All)]
        public void RPC_SetFaceId(int faceID)
        {
            FaceId = faceID;
            OnFaceIdSelected?.Invoke();
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_PlayerEnteredArena(PlayerController controller)
        {
            Controller = controller;
            Controller.ActivateController();
            GameLauncher.GameState = Enums.eGameState.Arena;
            OnPlayerEnterArena?.Invoke(Object.InputAuthority);
        }

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.All)]
        public void RPC_OnPlayerLoadLevel(PlayerRef playerRef)
        {
            OnPlayerLoadLevel?.Invoke(playerRef);
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_GameEnded()
        {
            s_GameManager.GameEnded();
            s_LevelManager.SpawnCounter();
        }

        public void InjectDependency(GameManager gameManager, LevelManager levelManager)
        {
            s_GameManager  = gameManager;
            s_LevelManager = levelManager;
        }

        private void OnDisable()
        {
            Players.Remove(this);
        }

        public void AddScore()
        {
            PlayerScore++;
        }
    }
}