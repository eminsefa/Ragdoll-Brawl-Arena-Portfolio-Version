using System;
using DG.Tweening;
using Project.Scripts.Essentials;
using Project.Scripts.Network;
using TMPro;
using UnityEngine;

namespace Project.Scripts.UI
{
    public class Screen_HUD : ScreenBase
    {
        [SerializeField] private TextMeshProUGUI m_BoostText;
        [SerializeField] private TextMeshProUGUI m_PlayerText;

        public void ShowBoost(BoostVariables.BoostData boostData)
        {
            m_BoostText.text = boostData.BoostText;
            Sequence mySequence = DOTween.Sequence();

            mySequence.Append(m_BoostText.transform.DOScale(1f, 1f)
                                         .From(0)
                                         .SetEase(Ease.OutBack))
                      .OnStart(()=>m_BoostText.gameObject.SetActive(true));
            mySequence.AppendInterval(0.75f);
            mySequence.AppendCallback(() => m_BoostText.gameObject.SetActive(false))
                      .OnKill(() => m_BoostText.gameObject.SetActive(false));
        }
        
        public void ShowPlayerEnter()
        {
            m_PlayerText.text = "Player " + RoomPlayer.Players.Count + "  Joined!";
            Sequence mySequence = DOTween.Sequence();
            mySequence.Append(m_PlayerText.transform.DOScale(1f, 0.5f)
                                         .From(0)
                                         .SetEase(Ease.OutBack))
                      .OnStart(()=>m_PlayerText.gameObject.SetActive(true));
            mySequence.AppendInterval(1.5f);
            mySequence.AppendCallback(() => m_PlayerText.gameObject.SetActive(false))
                      .OnKill(() => m_PlayerText.gameObject.SetActive(false));
        }
    }
}
