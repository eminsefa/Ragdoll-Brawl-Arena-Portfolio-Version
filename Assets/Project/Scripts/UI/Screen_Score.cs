using Project.Scripts.Character;
using Project.Scripts.Essentials;
using Project.Scripts.Network;
using UnityEngine;

namespace Project.Scripts.UI
{
    public class Screen_Score : ScreenBase
    {
        [SerializeField] private GameStartCooldownTimer m_GameStartCooldownTimer;
        [SerializeField] private FaceScoreGroup[]       m_FaceScoreGroups;

        private void OnEnable()
        {
            SetScore();
            m_GameStartCooldownTimer.gameObject.SetActive(true);

            var headSprites = GameConfig.Instance.Visual.HeadSprites;
            for (var i = 0; i < m_FaceScoreGroups.Length; i++)
            {
                m_FaceScoreGroups[i].SetImage(headSprites[i]);
            }
        }

        private void SetScore()
        {
            for (var i = 0; i < m_FaceScoreGroups.Length -1; i++)
            {
                var group = m_FaceScoreGroups[i];
                group.gameObject.SetActive(false);
                for (var i1 = 0; i1 <RoomPlayer.Players.Count; i1++)
                {
                    var player = RoomPlayer.Players[i1];
                    if (group.FaceId == player.FaceId)
                    {
                        group.gameObject.SetActive(true);
                        group.SetScore(player.PlayerScore);
                        break;
                    }
                }
            }
            var botScore = m_FaceScoreGroups[^1];

            if (RoomPlayer.Players.Count == 1)
            {
                botScore.gameObject.SetActive(true);
                botScore.SetScore(BotController.PlayerScore);
            }
            else botScore.gameObject.SetActive(false);
        }
    }
}