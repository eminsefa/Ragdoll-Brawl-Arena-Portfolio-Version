using System;
using Project.Scripts.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.UI
{
    public class FaceSelectGroup : MonoBehaviour
    {
        public event Action<FaceSelectGroup> OnIconSelected;

        private Enums.eFaceSelectState m_State;

        public int FaceId => m_FaceId;

        [SerializeField] private Image m_Image;
        [SerializeField] private int   m_FaceId;

        public void SetImage(Sprite sprite)
        {
            m_Image.sprite = sprite;
        }
    
        public void OnClicked()
        {
            UpdateIcon();
            switch (m_State)
            {
                case Enums.eFaceSelectState.UnAvailable:
                    ClientInfo.FaceId = -1;
                    return;
                case Enums.eFaceSelectState.Available:
                    OnIconSelected?.Invoke(this);
                    break;
                case Enums.eFaceSelectState.Selected:
                    OnIconSelected?.Invoke(this);
                    break;
            }

            SetImage();
        }

        public void UpdateIcon()
        {
            m_State = Enums.eFaceSelectState.Available;

            foreach (var roomPlayer in RoomPlayer.Players)
            {
                if (roomPlayer.FaceId == m_FaceId)
                {
                    if (!roomPlayer.Object.HasInputAuthority) m_State                         = Enums.eFaceSelectState.UnAvailable;
                    else if (m_State is not Enums.eFaceSelectState.UnAvailable) m_State = Enums.eFaceSelectState.Selected;
                }
            }

            SetImage();
        }
        
        private void SetImage()
        {
            m_Image.color = m_State switch
                            {
                                Enums.eFaceSelectState.Available   => new Color(1, 1, 1, 0.5f),
                                Enums.eFaceSelectState.UnAvailable => new Color(1, 1, 1, 0f),
                                Enums.eFaceSelectState.Selected    => new Color(1, 1, 1, 1f),
                                _                                        => m_Image.color
                            };
        }
    }
}