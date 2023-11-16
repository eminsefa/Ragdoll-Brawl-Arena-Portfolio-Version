using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EPOOutline;
using Fusion;
using Project.Scripts.Enums;
using Project.Scripts.Essentials;
using Project.Scripts.Managers;
using Project.Scripts.Objects;
using UnityEngine;

namespace Project.Scripts.Character
{
    public class BodyPart : NetworkBehaviour
    {
        private const string DETACHED_BODY_PART = "DetachedBodyPart";
        private const string BODY_PART          = "BodyPart";

        public event Action<BodyPart> OnBodyPartDetached;

        public float BodyHealth { get; set; } = 101;

        private TickTimer m_DamageTimer;
        public  TickTimer ImmuneTimer;

        private bool     m_IsSpawned;
        private bool     m_IsActive;
        private float    m_MaxHealth;
        private Sequence m_SeqColorChange;

        public  eBodyPart          BodyPartType    => m_BodyPartType;
        private CharacterVariables m_CharacterVars => GameConfig.Instance.Character;

        private static PoolManager  s_PoolManager;
        private static AudioManager s_AudioManager;

        [HideInInspector] [SerializeField] private bool m_IsBot;

        [SerializeField] private Outlinable     m_Outline;
        [SerializeField] private SpriteRenderer m_OutlineSprite;

        [SerializeField] private   PhysicsMaterial2D[] m_PhysicMaterials;
        [SerializeField] protected Joint2D             m_Joint;
        [SerializeField] private   CapsuleCollider2D   m_Col;
        [SerializeField] private   BodyPart            m_LowerBodyPart;

        [SerializeField] private Transform          m_BodyPartCenterTr;
        [SerializeField] private SpriteRenderer     m_SpriteRenderer;
        [SerializeField] private Rigidbody2D        m_Rb;
        [SerializeField] private NetworkRigidbody2D m_NetworkRb;
        [SerializeField] private eBodyPart          m_BodyPartType;

        public static void InjectDependencies(AudioManager audioManager, PoolManager poolManager)
        {
            s_AudioManager = audioManager;
            s_PoolManager  = poolManager;
        }

        public override void Spawned()
        {
            base.Spawned();

            m_MaxHealth                         = m_CharacterVars.BodyPart.BodyPartHealth[m_BodyPartType];
            m_NetworkRb.InterpolationDataSource = HasStateAuthority ? InterpolationDataSources.Auto : InterpolationDataSources.Snapshots;
        }

        public void InitBody()
        {
            BodyHealth = m_MaxHealth;

            SetColor(true);

            m_IsActive = BodyHealth > 0;
        }

        public void ActivateBody()
        {
            gameObject.SetActive(true);
            transform.GetChild(0).gameObject.SetActive(true);
            if (HasStateAuthority)
            {
                m_Rb.simulated = true;
                m_Col.enabled  = true;
            }
        }

        public async UniTask ResetBody(CharacterBase.BodyInitData data, bool firstSpawn)
        {
            while (!Object.IsValid)
            {
                await UniTask.Yield();
            }
            
            if (!firstSpawn)
            {
                gameObject.SetActive(true);
                transform.GetChild(0).gameObject.SetActive(true);
            }

            m_Rb.simulated = false;

            await ResetBodyPhysics(data);
        }

        private async UniTask ResetBodyPhysics(CharacterBase.BodyInitData data)
        {
            if (m_Joint != null)
            {
                Destroy(m_Joint);
            }

            transform.localScale = data.LocalScale;

            var parent = transform.parent;
            var pos    = parent.TransformPoint(data.LocalPos);
            var rot    = parent.rotation * data.LocalRot;

            m_NetworkRb.TeleportToPosition(pos, null,false);
            m_NetworkRb.TeleportToRotation(rot, null,false);
            m_NetworkRb.WriteVelocity(Vector2.zero);
            m_NetworkRb.WriteAngularVelocity(0);
            m_NetworkRb.WriteMass(1);
            m_NetworkRb.WriteDrag(0f);
            m_NetworkRb.WriteAngularDrag(1);
            gameObject.layer = LayerMask.NameToLayer(BODY_PART);

            await UniTask.NextFrame();
            if (!HasStateAuthority) return;
            
            m_Col.sharedMaterial = m_PhysicMaterials[0];
            if (m_BodyPartType == eBodyPart.Chest)
            {
                var joint = gameObject.AddComponent<FixedJoint2D>();
                joint.connectedBody                = data.ConnectedBody;
                joint.anchor                       = data.Anchor;
                m_Joint                            = (Joint2D) joint;
            }
            else
            {
                var joint = gameObject.AddComponent<HingeJoint2D>();
                joint.connectedBody                = data.ConnectedBody;
                joint.useLimits                    = true;
                joint.limits = new JointAngleLimits2D
                               {
                                   min = data.Limits.x,
                                   max = data.Limits.y,
                               };
                joint.anchor          = data.Anchor;
                m_Joint               = (Joint2D) joint;
            }
            
            await UniTask.NextFrame();
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if(!HasStateAuthority) return;
            if (BodyHealth <= 0) return;

            if (!m_DamageTimer.ExpiredOrNotRunning(Runner)) return;

            if (transform.root == col.transform.root) return;

            if (!col.gameObject.TryGetComponent(out BodyPart bodyPart)) return;

            if (bodyPart.BodyHealth <= 0 || !bodyPart.ImmuneTimer.ExpiredOrNotRunning(Runner)) return;

            m_DamageTimer = TickTimer.CreateFromSeconds(Runner, m_CharacterVars.BodyPart.DamageRate);

            var hitPos = col.GetContact(0).point;
            var dir    = (hitPos - (Vector2) m_Col.bounds.center).normalized;

            var vel = m_NetworkRb.ReadVelocity();

            var angleDirToVel = Vector2.Angle(dir, vel);

            var angleFactor = Mathf.Lerp(1, 0, angleDirToVel / 180f);
            var velMag      = vel.magnitude;
            velMag = Mathf.Min(velMag, m_CharacterVars.Movement.MaxVelocity);
            var speedFactor = Mathf.Lerp(m_CharacterVars.Fight.SpeedFactorRange.x,
                                         m_CharacterVars.Fight.SpeedFactorRange.y,
                                         velMag / m_CharacterVars.Movement.MaxVelocity);

            var angularVel = Mathf.Abs(m_NetworkRb.ReadAngularVelocity());
            angularVel = Mathf.Min(angularVel, m_CharacterVars.Movement.MaxAngularVelocity);

            var angularSpeedMult = Mathf.Lerp(m_CharacterVars.Fight.AngularSpeedFactorRange.x,
                                              m_CharacterVars.Fight.AngularSpeedFactorRange.y,
                                              angularVel / m_CharacterVars.Movement.MaxAngularVelocity);

            var damage = angleFactor * (speedFactor + angularSpeedMult);
            damage = Mathf.Min(damage, m_CharacterVars.Fight.MaxDamage);
            damage = ((int) (damage * 1000)) / 1000f;
            bodyPart.RPC_Hit(damage, dir, hitPos);
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_Hit(float damage, Vector2 dir, Vector2 hitPos)
        {
            if (BodyHealth <= 0) return;

            var damagePercentage = damage / m_CharacterVars.Fight.MaxDamage;

            if (HasStateAuthority)
            {
                var forceMult = Mathf.Lerp(m_CharacterVars.Fight.ForceRange.x, m_CharacterVars.Fight.ForceRange.y, damagePercentage);
                m_Rb.AddForceAtPosition(dir * forceMult, hitPos, ForceMode2D.Impulse);

                var vel = m_Rb.velocity;
                m_Rb.velocity = Vector2.ClampMagnitude(vel, m_CharacterVars.Movement.MaxVelocity);
                
                ImmuneTimer   = TickTimer.CreateFromSeconds(Runner, m_CharacterVars.BodyPart.ImmuneTime);
            }

            SpawnHitParticle(damagePercentage, dir, hitPos);

            s_AudioManager.PlayHitAudio(eAudioType.Hit, damagePercentage);

                 BodyHealth -= damage;

            SetColor(false);
            if (BodyHealth <= 0) Detach();
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_UpdateHealth(float health)
        {
            BodyHealth = health;
            if (BodyHealth > 0)
            {
                m_IsActive = true;
                gameObject.SetActive(true);
                SetColor(false);
            }
            else
            {
                m_IsActive = false;
                DeactivateSelf();
            }
        }

        private void SetColor(bool start = false)
        {
            if (BodyHealth <= 0) return;

            var t = start ? 1 : BodyHealth / m_MaxHealth;

            var spriteColor = Color.Lerp(m_CharacterVars.BodyPart.SpriteHealthColorRange[0], m_CharacterVars.BodyPart.SpriteHealthColorRange[1], 1 - t);

            var outlineColor = m_CharacterVars.BodyPart.OutlineColorGradient.Evaluate(1 - t);

            m_SpriteRenderer.color = spriteColor;

            if (m_BodyPartType == eBodyPart.Head) m_Outline.OutlineParameters.Color = outlineColor;
            else m_OutlineSprite.color                                              = outlineColor;
        }

        private void Detach()
        {
            s_AudioManager.PlayHitAudio(eAudioType.Detach);
            Disconnect(false);

            OnBodyPartDetached?.Invoke(this);
        }

        public void Disconnect(bool dead)
        {
            if (!m_IsActive) return;
            m_IsActive = false;

            BodyHealth = -1;

            if (m_LowerBodyPart != null) m_LowerBodyPart.Disconnect(dead);

            SpawnBreakParticle();

            if (m_SeqColorChange != null && DOTween.IsTweening(m_SeqColorChange)) m_SeqColorChange.Kill();

            m_SeqColorChange = DOTween.Sequence();
            m_SeqColorChange.Append(m_SpriteRenderer.DOColor(m_CharacterVars.BodyPart.DetachedEndColor, m_CharacterVars.BodyPart.DetachedScaleDuration))
                            .Join(m_BodyPartType == eBodyPart.Head
                                      ? m_Outline.OutlineParameters.DOColor(m_CharacterVars.BodyPart.DetachedEndColor, m_CharacterVars.BodyPart.DetachedScaleDuration)
                                      : m_OutlineSprite.DOColor(m_CharacterVars.BodyPart.DetachedEndColor, m_CharacterVars.BodyPart.DetachedScaleDuration))
                            .Join(transform.DOScale(0, m_CharacterVars.BodyPart.DetachedScaleDuration)
                                           .SetEase(m_CharacterVars.BodyPart.DetachedScaleEase)
                                           .OnComplete(DeactivateSelf))
                            .SetDelay(m_CharacterVars.BodyPart.DetachedScaleDelay);

            if (!HasStateAuthority) return;

            if (m_Joint) m_Joint.enabled = false;
            m_Col.sharedMaterial = m_PhysicMaterials[1];
            gameObject.layer     = LayerMask.NameToLayer(DETACHED_BODY_PART);

            var vel = (Vector2) transform.up * (dead
                                                    ? m_CharacterVars.BodyPart.DetachForceRange.y
                                                    : Mathf.Lerp(m_CharacterVars.BodyPart.DetachForceRange.x,
                                                                 m_CharacterVars.BodyPart.DetachForceRange.y,
                                                                 m_NetworkRb.ReadVelocity().magnitude / m_CharacterVars.Movement.MaxVelocity));

            m_Rb.AddForceAtPosition(vel, transform.root.position, ForceMode2D.Impulse);

            m_Rb.velocity        = Vector2.ClampMagnitude(vel, m_CharacterVars.BodyPart.DetachedMaxVelocity);
            m_Rb.angularVelocity = Mathf.Clamp(Mathf.Abs(m_Rb.angularVelocity), 0, m_CharacterVars.BodyPart.DetachedMaxAngularVelocity) * Mathf.Sign(m_Rb.angularVelocity);
        }

        private void SpawnHitParticle(float damagePercentage, Vector3 dir, Vector3 hitPos)
        {
            s_PoolManager.HitParticlePool.Get(out ProjectPoolObject poolObject);

            var particleTr = poolObject.transform;

            particleTr.position   =  hitPos;
            particleTr.localScale *= Mathf.Lerp(0.25f, 4f, damagePercentage);
            particleTr.forward    =  -dir;
            if (poolObject.TryGetComponent(out ParticleController particleController))
            {
                particleController.SetParticleEmission(damagePercentage);
            }
        }

        private void SpawnBreakParticle()
        {
            s_PoolManager.BreakParticlePool.Get(out ProjectPoolObject obj2);

            var tr          = transform;
            var particleTr2 = obj2.transform;

            particleTr2.position = tr.position;
            particleTr2.right    = tr.up;
            particleTr2.SetParent(m_NetworkRb.InterpolationTarget);
        }

        public void OnBoost(eBoostType boostType, bool active)
        {
            switch (boostType)
            {
                case eBoostType.BulletBody:
                    OnBulletBody(active);
                    break;
                case eBoostType.BigHead:
                    if (active) transform.localScale = 1.25f * Vector3.one;
                    else transform.localScale        = 0.75f * Vector3.one;
                    break;
                case eBoostType.Rotate:
                    return;
            }
        }

        private void OnBulletBody(bool active)
        {
            if (m_BodyPartType is eBodyPart.Head or eBodyPart.Chest) return;

            if (BodyHealth <= 0) return;

            if (active)
            {
                m_Col.enabled = false;
                m_NetworkRb.WriteMass(0.01f);
                // m_Rb.mass     = 0.01f;
                transform.GetChild(0).gameObject.SetActive(false);
            }
            else
            {
                m_NetworkRb.WriteMass(1);
                // m_Rb.mass     = 1f;
                m_Col.enabled = true;
                transform.GetChild(0).gameObject.SetActive(true);
            }
        }

        public void DeactivateSelf()
        {
            gameObject.SetActive(false);
            m_Rb.simulated       = false;
            m_Rb.velocity        = Vector2.zero;
            m_Rb.angularVelocity = 0;
        }
    }
}