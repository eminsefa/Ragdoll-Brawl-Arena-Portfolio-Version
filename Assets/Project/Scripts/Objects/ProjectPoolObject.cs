using Project.Scripts.Managers;
using UnityEngine;
using UnityEngine.Pool;

namespace Project.Scripts.Objects
{
    public class ProjectPoolObject : MonoBehaviour
    {
        private bool m_IsInPool;

        [SerializeField] protected Enums.ePoolType m_PoolType;

        private IObjectPool<ProjectPoolObject> m_Pool;

        private void OnEnable()
        {
            m_IsInPool = false;
        }

        private void Start()
        {
            GameManager.OnGameEnded += OnGameEnded;
        }

        private void OnDestroy()
        {
            GameManager.OnGameEnded -= OnGameEnded;
        }

        public void Init(IObjectPool<ProjectPoolObject> pool)
        {
            m_Pool = pool;
        }

        private void OnGameEnded()
        {
            if (!m_IsInPool)
            {
                m_Pool.Release(this);
                m_IsInPool = true;
            }
        }

        public void OnParticleSystemStopped()
        {
            if (!m_IsInPool)
            {
                m_Pool.Release(this);
                m_IsInPool = true;
            }
        }
    }
}