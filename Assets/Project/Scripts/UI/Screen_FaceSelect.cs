using System;
using Project.Scripts.Essentials;
using Project.Scripts.Network;
using UnityEngine;

namespace Project.Scripts.UI
{
    public class Screen_FaceSelect : ScreenBase
    {
        public static event Action OnIconSelectionCompleted;

        [SerializeField] private FaceSelectGroup[] m_FaceSelectGroups;

        private void OnEnable()
        {
            var headSprites = GameConfig.Instance.Visual.HeadSprites;
            for (var i = 0; i < m_FaceSelectGroups.Length; i++)
            {
                m_FaceSelectGroups[i].OnIconSelected += OnIconSelected;
                m_FaceSelectGroups[i].UpdateIcon();
                m_FaceSelectGroups[i].SetImage(headSprites[i]);
            }
            RoomPlayer.OnFaceIdSelected += UpdateIcons;
        }

        private void OnDisable()
        {
            for (var i = 0; i < m_FaceSelectGroups.Length; i++)
            {
                m_FaceSelectGroups[i].OnIconSelected -= OnIconSelected;
            }
            RoomPlayer.OnFaceIdSelected -= UpdateIcons;
        }

        private void OnIconSelected(FaceSelectGroup faceSelectGroup)
        {
            if (faceSelectGroup.FaceId == ClientInfo.FaceId)
            {
                OnIconSelectionCompleted?.Invoke();
            }
            ClientInfo.FaceId = faceSelectGroup.FaceId;

            if (RoomPlayer.Local != null)
            {
                RoomPlayer.Local.RPC_SetFaceId(faceSelectGroup.FaceId);
            }
        }

        private void UpdateIcons()
        {
            for (var i = 0; i < m_FaceSelectGroups.Length; i++)
            {
                m_FaceSelectGroups[i].UpdateIcon();
            }
        }
    }
}
