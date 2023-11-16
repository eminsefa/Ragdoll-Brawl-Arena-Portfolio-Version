using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.UI
{
    public class FaceScoreGroup : MonoBehaviour
    {
        public int Score  => m_Score;
        public int FaceId => m_FaceId;

        private int m_Score;

        [SerializeField] private int             m_FaceId;
        [SerializeField] private Image             m_Image;
        [SerializeField] private TextMeshProUGUI m_ScoreText;

        public void SetScore(int score)
        {
            m_Score          = score;
            m_ScoreText.text = m_Score.ToString();
        }

        public void SetImage(Sprite sprite)
        {
            m_Image.sprite = sprite;
        }
    }
}