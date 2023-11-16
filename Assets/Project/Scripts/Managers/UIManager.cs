using System;
using Cysharp.Threading.Tasks;
using Fusion;
using Project.Scripts.Essentials;
using Project.Scripts.Network;
using Project.Scripts.UI;
using UnityEngine;
using Zenject;

namespace Project.Scripts.Managers
{
    public class UIManager : MonoBehaviour
    {
        [Inject] private LevelManager m_LevelManager;

        [SerializeField] private ScreenDictionary m_ScreenDictionary;

        private void OnEnable()
        {
            Screen_FaceSelect.OnIconSelectionCompleted          += OnIconSelectionCompleted;
            RoomPlayer.OnRoomPlayerCreated                      += OnRoomPlayerCreated;
            RoomPlayer.OnPlayerEnterArena                       += OnPlayerEnterArena;
            GameStartCooldownTimer.OnGameStartCooldownCompleted += OnGameStartCooldownCompleted;

            DontDestroyOnLoad(gameObject);
        }

        private void OnDisable()
        {
            Screen_FaceSelect.OnIconSelectionCompleted          -= OnIconSelectionCompleted;
            RoomPlayer.OnRoomPlayerCreated                      -= OnRoomPlayerCreated;
            RoomPlayer.OnPlayerEnterArena                       -= OnPlayerEnterArena;
            GameStartCooldownTimer.OnGameStartCooldownCompleted -= OnGameStartCooldownCompleted;
        }

        private async void OnRoomPlayerCreated(RoomPlayer i_RoomPlayer)
        {
            await UniTask.NextFrame();
            m_ScreenDictionary[Enums.eScreenType.FaceSelect].Open();
        }

        private void OnIconSelectionCompleted()
        {
            m_ScreenDictionary[Enums.eScreenType.FaceSelect].Close();
            m_LevelManager.LoadArena();
        }

        private void OnPlayerEnterArena(PlayerRef i_PlayerRef)
        {
            m_ScreenDictionary[Enums.eScreenType.HUD].Open();
        }

        public void ShowScoreScreen()
        {
            m_ScreenDictionary[Enums.eScreenType.HUD].Close();
            m_ScreenDictionary[Enums.eScreenType.Score].Open();
        }

        private void OnGameStartCooldownCompleted()
        {
            m_ScreenDictionary[Enums.eScreenType.Score].Close();
            m_ScreenDictionary[Enums.eScreenType.HUD].Open();
        }

        public void ShowBoostEffect(Enums.eBoostType i_BoostType)
        {
            var data = GameConfig.Instance.Boost.BoostDictionary[i_BoostType];
            ((Screen_HUD) m_ScreenDictionary[Enums.eScreenType.HUD]).ShowBoost(data);
        }

        [Serializable] public class ScreenDictionary : UnitySerializedDictionary<Enums.eScreenType, ScreenBase> { }
    }
}