using Project.Scripts.Essentials;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Project.Scripts.Managers
{
    public class FPSManager : MonoBehaviour
    {
        private const float DEFAULT_DELTA_TIME = 0.01667f; // => 1/60

        private float m_LastTime = 0;
        private float m_DeltaTime;
        private float m_DeltaTimeSum;

        private float m_FPSSumPreviousLevel;
        private float m_CurrTime => Time.time;

        [SerializeField, Range(1, 30)] private float m_UpdatesPerSecond = 5;

        [ShowInInspector, ReadOnly] public static int FPSAverageLastUpdate = 0;
        [ShowInInspector, ReadOnly] public static int FPSTicksLastUpdate   = 0;

        private void OnEnable()
        {
            Application.targetFrameRate = GameConfig.Instance.TargetFrameRate;

            FPSAverageLastUpdate = 0;
            m_LastTime           = m_CurrTime;

            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            m_DeltaTime = m_CurrTime - m_LastTime;

            if (m_DeltaTime <= 0) return;

            m_LastTime     =  m_CurrTime;
            m_DeltaTimeSum += m_DeltaTime;

            FPSTicksLastUpdate++;
            if (m_DeltaTimeSum > 1 / m_UpdatesPerSecond)
            {
                float fps = FPSTicksLastUpdate / m_DeltaTimeSum;
                FPSAverageLastUpdate = Mathf.RoundToInt(fps);

                m_DeltaTimeSum = FPSTicksLastUpdate = 0;
            }
        }
    }
}