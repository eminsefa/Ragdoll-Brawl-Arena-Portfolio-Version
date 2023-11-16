using System;
using TMPro;
using UnityEngine;

namespace Project.Scripts.UI
{
    public class GameStartCooldownTimer : MonoBehaviour
    {
        public static event Action OnGameStartCooldownCompleted;

        private float m_CooldownTimer = 4f;

        [SerializeField] private TextMeshProUGUI m_CooldownText;

        private void OnEnable()
        {
            m_CooldownTimer = 4f;
        }

        private void Update()
        {
            m_CooldownTimer -= Time.deltaTime;
            var time = Mathf.CeilToInt(m_CooldownTimer);
            SetText(time);

            if (time == 0)
            {
                OnGameStartCooldownCompleted?.Invoke();
                gameObject.SetActive(false);
            }
        }

        private void SetText(int count)
        {
            m_CooldownText.text = count.ToString();
        }
    }
}