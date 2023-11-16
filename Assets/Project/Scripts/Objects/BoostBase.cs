using System;
using Fusion;
using Project.Scripts.Character;
using Project.Scripts.Network;
using UnityEngine;

namespace Project.Scripts.Objects
{
    public class BoostBase : NetworkBehaviour
    {
        public static event Action OnBoostCollected;
        public static BoostBase    Instance;

        public bool IsActive { get; private set; }

        public override void Spawned()
        {
            IsActive = true;
            Instance = this;
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (!HasStateAuthority) return;
            if (!IsActive) return;

            if (!col.transform.root.TryGetComponent(out CharacterBase character)) return;

            HasCollected(character);
        }

        private void HasCollected(CharacterBase controller)
        {
            IsActive = false;
            RPC_DeactivateSelf();

            Array                  values      = Enum.GetValues(typeof(Enums.eBoostType));
            Enums.eBoostType randomBoost = (Enums.eBoostType) values.GetValue(UnityEngine.Random.Range(1, values.Length));
            OnBoostCollected?.Invoke();

            controller.RPC_CollectedBoost(randomBoost);
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_Init(Vector3 spawnPos)
        {
            transform.position = spawnPos;
            IsActive           = true;
            gameObject.SetActive(true);
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_DeactivateSelf()
        {
            gameObject.SetActive(false);

            IsActive = false;
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_UpdatePosition(bool isActive, Vector3 position)
        {
            IsActive = isActive;
            gameObject.SetActive(IsActive);
            transform.position = position;
        }
    }
}