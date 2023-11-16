using System.Collections;
using Fusion;
using Project.Scripts.Essentials;
using Project.Scripts.Network;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scripts.Managers
{
    public class GravityManager : NetworkBehaviour
    {
        [Networked] private Vector2 Gravity { get; set; } =Vector2.down * -9.81f;

        private bool             m_IsActive;
        private GravityVariables m_GravityVars;
    
        private void OnEnable()
        {
            m_GravityVars           = GameConfig.Instance.Gravity;
            
            DontDestroyOnLoad(gameObject);
        }

        public override void Spawned()
        {
            base.Spawned();
            m_IsActive              = true;

            SetGravity();
            if (GameLauncher.NetworkRunner.GameMode == GameMode.Host)
            {
                StartCoroutine(GravityChangeSequence());
            }
        }

        private IEnumerator GravityChangeSequence()
        {
            while (m_IsActive)
            {
                yield return new WaitForSeconds(Random.Range(m_GravityVars.ChangeDurationRange.x, m_GravityVars.ChangeDurationRange.y));
                // Gravity = Random.insideUnitCircle;
                var rand = Random.Range(0, 4);

                Gravity = rand switch
                          {
                              0 => Gravity = Vector2.down,
                              1 => Gravity = Vector2.up,
                              2 => Gravity = Vector2.right,
                              3 => Gravity = Vector2.left,
                              _ => Gravity = Vector2.down,
                          };
            
                var strength = Random.Range(m_GravityVars.StrengthRange.x, m_GravityVars.StrengthRange.y);
                Gravity *= strength;
                
                RPC_GravityChanged();
              
            }
        }
        
        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPC_GravityChanged()
        {
            SetGravity();
        }

        private void SetGravity()
        {
            Physics2D.gravity = Gravity;
            Physics.gravity   = Gravity ;
            
            transform.localRotation = Quaternion.LookRotation(Gravity, Vector3.forward);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }
    }
}