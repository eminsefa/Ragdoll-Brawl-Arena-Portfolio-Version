using Fusion;
using Project.Scripts.Network;
using UnityEngine;

namespace Project.Scripts.Managers
{
    public class CameraManager : MonoBehaviour
    {
        private const float DEFAULT_ASPECT = 1.7778f;

        [SerializeField] private Camera m_BlockCamera;
        [SerializeField] private Camera m_MainCamera;

        private void Awake()
        {
            setCameraSize();
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameManager.OnGameEnded       += OnGameEnded;
            RoomPlayer.OnPlayerEnterArena += OnPlayerEnterArena;
        }

        private void OnDisable()
        {
            RoomPlayer.OnPlayerEnterArena -= OnPlayerEnterArena;
            GameManager.OnGameEnded       -= OnGameEnded;
        }

        private void setCameraSize()
        {
            float currentAspect = (float) Screen.width / Screen.height;
            var   mult          = DEFAULT_ASPECT       / currentAspect;
            if (mult < 1) return;

            float newOrthographicSize = m_MainCamera.orthographicSize * mult;

            m_MainCamera.orthographicSize  = newOrthographicSize;
            m_BlockCamera.orthographicSize = newOrthographicSize;
        }

        private void OnGameEnded()
        {
            m_BlockCamera.depth = 1;
        }

        private void OnPlayerEnterArena(PlayerRef playerRef)
        {
            if (playerRef == GameLauncher.NetworkRunner.LocalPlayer) m_BlockCamera.depth = -2;
        }
    }
}