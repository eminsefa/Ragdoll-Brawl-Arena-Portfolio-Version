using System;
using Cysharp.Threading.Tasks;
using Project.Scripts.Enums;
using Project.Scripts.Essentials;
using Project.Scripts.Objects;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;

namespace Project.Scripts.Managers
{
    public class PoolManager : MonoBehaviour
    {
        [SerializeField] private AssetReference                    m_HitParticleAsset;
        [SerializeField] private AssetReference                    m_BreakParticleAsset;
        
         PoolDictionary<ProjectPoolObject> m_PoolDictionary;

        public IObjectPool<ProjectPoolObject> BreakParticlePool { get; private set; }
        public IObjectPool<ProjectPoolObject> HitParticlePool   { get; private set; }

        private void Awake()
        {
            SetPoolDictionary();
            
            transform.hierarchyCapacity = 256;
            BreakParticlePool           = new ObjectPool<ProjectPoolObject>(CreatePooledBreakParticle, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, false, 100, 100);
            HitParticlePool             = new ObjectPool<ProjectPoolObject>(CreatePooledHitParticle,   OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, false, 100, 100);
            
            DontDestroyOnLoad(gameObject);
        }

        private  void SetPoolDictionary()
        {
            m_PoolDictionary = new PoolDictionary<ProjectPoolObject>();
            m_HitParticleAsset.LoadAssetAsync<GameObject>().Completed += (op) =>
                                                                         {
                                                                             m_PoolDictionary.Add(ePoolType.HitParticle, op.Result.GetComponent<ProjectPoolObject>());
                                                                         };
            m_BreakParticleAsset.LoadAssetAsync<GameObject>().Completed += (op) =>
                                                                           {
                                                                               m_PoolDictionary.Add(ePoolType.BreakParticle, op.Result.GetComponent<ProjectPoolObject>());
                                                                           };
        }

        private ProjectPoolObject CreatePooledBreakParticle()
        {
            var ps = Instantiate(m_PoolDictionary[Enums.ePoolType.BreakParticle], transform);
            ps.Init(BreakParticlePool);

            return ps;
        }

        private ProjectPoolObject CreatePooledHitParticle()
        {
            var ps = Instantiate(m_PoolDictionary[Enums.ePoolType.HitParticle], transform);
            ps.Init(HitParticlePool);

            return ps;
        }

        private void OnReturnedToPool(ProjectPoolObject poolObject)
        {
            poolObject.transform.SetParent(transform);
            poolObject.transform.localScale = Vector3.one;
            poolObject.gameObject.SetActive(false);
        }

        private void OnTakeFromPool(ProjectPoolObject poolObject)
        {
            poolObject.gameObject.SetActive(true);
        }

        private void OnDestroyPoolObject(ProjectPoolObject poolObject)
        {
            Destroy(poolObject.gameObject);
        }
    }


    [Serializable] public class PoolDictionary<T> : UnitySerializedDictionary<Enums.ePoolType, T> { }
}