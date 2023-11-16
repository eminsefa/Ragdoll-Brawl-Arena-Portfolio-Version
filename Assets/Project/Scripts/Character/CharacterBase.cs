using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using Project.Scripts.Enums;
using Project.Scripts.Essentials;
using Project.Scripts.Managers;
using Project.Scripts.Network;
using UnityEngine;

namespace Project.Scripts.Character
{
    public class CharacterBase : NetworkBehaviour
    {
        public static List<CharacterBase>         SpawnedCharacters = new List<CharacterBase>();
        public static event Action<CharacterBase> OnCharacterDead;

        private int m_FaceId;

        public bool IsReset  { get; set; }
        public bool IsInit   { get; set; }
        public bool IsActive { get; set; }
        public bool IsSpawned { get; set; }

        private static UIManager    s_UIManager;
        private static AudioManager s_AudioManager;

        [SerializeField] private SpriteRenderer m_HeadSpriteRenderer;
        [SerializeField] private SpriteRenderer m_BodyBoostSprite;

        [SerializeField]                   private   Collider2D[]      m_Cols;
        [SerializeField]                   private   BodyPart[]        m_BodyParts;
        [SerializeField]                   protected CharacterMovement m_Movement;
        
        [HideInInspector][SerializeField] private bool           m_IsBot;
        [HideInInspector][SerializeField] public  BodyInitData[] m_BodyInitData;

        [Serializable]
        public struct BodyInitData
        {
            public Rigidbody2D ConnectedBody;
            public Vector2     Limits;
            public Vector2     Anchor;
            public Vector2     ConnectedAnchor;
            public Vector2     LocalPos;
            public Quaternion  LocalRot;
            public Vector3     LocalScale;
        }

        public static void InjectDependencies(UIManager uiManager, AudioManager audioManager)
        {
            s_UIManager    = uiManager;
            s_AudioManager = audioManager;
        }
        
        public override void Spawned()
        {
            base.Spawned();
            m_Movement.SetInterpolationSource(HasStateAuthority);
            if (Object.HasInputAuthority) RPC_Spawned();
        }
        
        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.All)]
        public void RPC_Spawned()
        {
            IsSpawned = true;
        }
        
        protected virtual void OnEnable()
        {
            GameManager.OnGameEnded += OnGameEnded;

            for (var i = 0; i < m_BodyParts.Length; i++)
            {
                m_BodyParts[i].OnBodyPartDetached += OnBodyPartDetached;
            }

            SpawnedCharacters.Add(this);
        }

        protected virtual void OnDisable()
        {
            GameManager.OnGameEnded -= OnGameEnded;

            for (var i = 0; i < m_BodyParts.Length; i++)
            {
                m_BodyParts[i].OnBodyPartDetached -= OnBodyPartDetached;
            }

            SpawnedCharacters.Remove(this);
        }

        private void OnGameEnded()
        {
            IsActive = false;
            IsReset  = false;
            IsInit   = false;
            StopAllCoroutines();
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_Init(Vector3 position)
        {
            Init(position);
        }

        public async void Init(Vector3 position)
        {
            await m_Movement.Init(position);

            for (var i = 0; i < m_BodyParts.Length; i++)
            {
                m_BodyParts[i].InitBody();
            }

            if (HasInputAuthority)
            {
                RPC_SetFaceID(RoomPlayer.Local.FaceId);
                await UniTask.NextFrame();
                RPC_SetReady();
            }

            if (m_IsBot)
            {
                await UniTask.NextFrame();
                IsInit = true;
            }
        }

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.All)]
        public void RPC_SetReady()
        {
            IsInit = true;
        }

        public void ActivateController()
        {
            if (HasStateAuthority) m_Movement.Activate();
            for (var i = 0; i < m_BodyParts.Length; i++)
            {
                m_BodyParts[i].ActivateBody();
            }

            IgnoreBodyPartsCollision();
            IsActive = true;
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public async void RPC_ResetController(bool firstSpawn = false)
        {
            await ResetController(firstSpawn);
        }

        public async UniTask ResetController(bool firstSpawn = false)
        {
            m_BodyBoostSprite.gameObject.SetActive(false);

            if (!firstSpawn) await m_Movement.ResetMovement();

            // UniTask[] tasks = new UniTask[m_BodyParts.Length];
            for (var i = 0; i < m_BodyParts.Length; i++)
            {
                await m_BodyParts[i].ResetBody(m_BodyInitData[i], firstSpawn);
                // tasks[i]=m_BodyParts[i].ResetBody(m_BodyInitData[i], firstSpawn);
            }

            // await UniTask.WhenAll(tasks);
            IsReset = true;
        }

        private void OnBodyPartDetached(BodyPart bodyPart)
        {
            if (!HasStateAuthority) return;
            if (bodyPart.BodyPartType is eBodyPart.Head or eBodyPart.Chest) RPC_Dead();
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPC_Dead()
        {
            for (var i = 0; i < m_BodyParts.Length; i++)
            {
                m_BodyParts[i].Disconnect(true);
            }

            IsActive = false;
            IsReset  = false;
            IsInit   = false;

            StopAllCoroutines();
            OnCharacterDead?.Invoke(this);
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_CollectedBoost(eBoostType boostType)
        {
            StartCoroutine(StartBoostSequence(boostType));
        }

        private IEnumerator StartBoostSequence(eBoostType boostType)
        {
            float duration = GameConfig.Instance.Boost.BoostDictionary[boostType].Duration;
            switch (boostType)
            {
                case eBoostType.BigHead:
                    m_BodyParts[1].OnBoost(eBoostType.BigHead, true);
                    break;
                case eBoostType.Speed:
                    m_Movement.SetSpeedBoost(true);
                    break;
                case eBoostType.Rotate:
                    m_Movement.SetRotateBoost(true);
                    break;
                case eBoostType.BulletBody:
                    for (var i = 0; i < m_BodyParts.Length; i++)
                    {
                        m_BodyParts[i].OnBoost(eBoostType.BulletBody, true);
                    }

                    break;
            }

            if (HasInputAuthority)
            {
                s_UIManager.ShowBoostEffect(boostType);
                eAudioType audioType = boostType is eBoostType.Rotate or eBoostType.Speed ? eAudioType.BoostCollect : eAudioType.BoostEnd;
                s_AudioManager.PlayFrontAudio(audioType);
            }

            m_BodyBoostSprite.gameObject.SetActive(true);

            yield return new WaitForSeconds(duration);

            switch (boostType)
            {
                case eBoostType.BigHead:
                    m_BodyParts[1].OnBoost(eBoostType.BigHead, false);
                    break;
                case eBoostType.Speed:
                    m_Movement.SetSpeedBoost(false);
                    break;
                case eBoostType.Rotate:
                    m_Movement.SetRotateBoost(false);
                    break;
                case eBoostType.BulletBody:
                    for (var i = 0; i < m_BodyParts.Length; i++)
                    {
                        m_BodyParts[i].OnBoost(eBoostType.BulletBody, false);
                    }

                    break;
            }

            if (HasInputAuthority)
            {
                eAudioType audioType = boostType is eBoostType.Rotate or eBoostType.Speed ? eAudioType.BoostEnd : eAudioType.BoostCollect;
                s_AudioManager.PlayFrontAudio(audioType);
            }

            m_BodyBoostSprite.gameObject.SetActive(false);
        }

        public void UpdateCharacterValues()
        {
            RPC_SetFaceID(m_FaceId, IsActive);
            for (var i = 0; i < m_BodyParts.Length; i++)
            {
                m_BodyParts[i].RPC_UpdateHealth(m_BodyParts[i].BodyHealth);
            }
        }

        [Rpc(sources: RpcSources.StateAuthority | RpcSources.InputAuthority, targets: RpcTargets.All)]
        public void RPC_SetFaceID(int id, bool active = true)
        {
            IsActive = active;
            SetFaceId(id);
        }

        public void SetFaceId(int id)
        {
            m_FaceId = id;

            m_HeadSpriteRenderer.sprite = GameConfig.Instance.Visual.HeadSprites[id];
        }

        private void IgnoreBodyPartsCollision()
        {
            for (var i = 0; i < m_Cols.Length; i++)
            {
                for (var i1 = 0; i1 < m_Cols.Length; i1++)
                {
                    Physics2D.IgnoreCollision(m_Cols[i], m_Cols[i1]);
                }
            }
        }
    }
}