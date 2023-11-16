using Cysharp.Threading.Tasks;
using Fusion;
using Project.Scripts.Essentials;
using UnityEngine;

namespace Project.Scripts.Character
{
    public class CharacterMovement : MonoBehaviour
    {
        private float m_SpeedBoost = 1;
        private bool  m_IsOnRotateBoost;

        private CharacterVariables m_CharacterVariables;

        [SerializeField] private Rigidbody2D        m_Rb;
        [SerializeField] private NetworkRigidbody2D m_NetworkRb;

        private void OnEnable()
        {
            m_CharacterVariables = GameConfig.Instance.Character;
            m_Rb.centerOfMass    = transform.position;
        }

        public void SetInterpolationSource(bool hasStateAuthority)
        {
            m_NetworkRb.InterpolationDataSource = hasStateAuthority ? NetworkBehaviour.InterpolationDataSources.Predicted : NetworkBehaviour.InterpolationDataSources.Snapshots;
        }
        
        public async UniTask ResetMovement()
        {
            m_SpeedBoost                        = 1;
            m_IsOnRotateBoost                   = false;

            m_Rb.simulated = false;
            m_NetworkRb.WriteVelocity(Vector2.zero);
            m_NetworkRb.WriteAngularVelocity(0);
            m_NetworkRb.TeleportToPosition(Vector3.zero, null,false);
            m_NetworkRb.TeleportToRotation(Quaternion.identity, null, false);
            await UniTask.NextFrame();
        }

        public async UniTask Init(Vector3 position)
        {
            m_NetworkRb.TeleportToPosition(position, null,false);
            m_NetworkRb.TeleportToRotation(Quaternion.identity, null,false);
            await UniTask.NextFrame();
        }

        public void Activate()
        {
            m_Rb.simulated = true;
        }
        
        public void Move(Vector2 moveVector, float deltaTime)
        {
            var move  = moveVector                              * deltaTime;
            var speed = m_CharacterVariables.Movement.MoveSpeed * move.magnitude *m_SpeedBoost;
            m_Rb.AddForce(move.normalized                       * speed , ForceMode2D.Impulse);
            m_Rb.velocity = Vector2.ClampMagnitude(m_Rb.velocity, m_CharacterVariables.Movement.MaxVelocity * m_SpeedBoost);

            float targetAngle = Mathf.Atan2(moveVector.y, moveVector.x) * Mathf.Rad2Deg;

            float smoothedAngle = 0f;

            if (m_IsOnRotateBoost) smoothedAngle = m_NetworkRb.ReadRigidbodyRotation() + (Physics2D.maxRotationSpeed*3 * deltaTime);
            else
            {
                var t           = m_NetworkRb.ReadVelocity().magnitude / m_CharacterVariables.Movement.MaxVelocity;
                var speedMult   = Mathf.Lerp(0.5f, 1, 1/t);
                var rotateSpeed = m_CharacterVariables.Movement.RotateSpeed * m_SpeedBoost * speedMult * deltaTime;

                var targetRotation  = Mathf.LerpAngle(m_NetworkRb.ReadRigidbodyRotation(), targetAngle - 90, rotateSpeed);
                var angleDifference = Mathf.DeltaAngle(m_NetworkRb.ReadRigidbodyRotation(), targetRotation);
                var maxAngleChange  = m_CharacterVariables.Movement.MaxAngularVelocity * deltaTime;
                angleDifference = Mathf.Clamp(angleDifference, -maxAngleChange, maxAngleChange);

                smoothedAngle = m_NetworkRb.ReadRigidbodyRotation() + angleDifference;
            }

            m_Rb.MoveRotation(smoothedAngle);
        }

        public void SetSpeedBoost(bool active)
        {
            m_SpeedBoost = active ? 1.75f : 1;
        }

        public void SetRotateBoost(bool active)
        {
            m_IsOnRotateBoost = active;
        }
    }
}