using Fusion;
using Project.Scripts.Attributes;
using Project.Scripts.Essentials;
using Project.Scripts.InputProject;
using Project.Scripts.Network;
using UnityEngine;

namespace Project.Scripts.Managers
{
    [ExecutionOrder(Enums.eExecutionOrder.InputManager)]
    public class InputManager : MonoBehaviour
    {
        private bool m_IsTouching;

        private Vector2 m_InputSlowDownVelocity;
        private Vector2 m_InputSpeedUpVelocity;
        private Vector2 m_InputDelta;

        private InputVariables m_InputVars => GameConfig.Instance.Input;

        private void OnEnable()
        {
            InputButton.OnInputDown  += OnInputDown;
            InputButton.OnInputUp    += OnInputUp;
            InputButton.OnInputDelta += OnInputDrag;
            GameManager.OnGameEnded  += OnGameEnded;
            
            DontDestroyOnLoad(gameObject);
        }

        private void OnDisable()
        {
            InputButton.OnInputDown     -= OnInputDown;
            InputButton.OnInputUp       -= OnInputUp;
            InputButton.OnInputDelta    -= OnInputDrag;
            GameManager.OnGameEnded -= OnGameEnded;
        }

        private void OnInputDown()
        {
            m_IsTouching = true;
        }

        private void OnInputUp()
        {
            m_IsTouching = false;
        }

        private void OnInputDrag(Vector2 delta)
        {
            if (delta.sqrMagnitude < m_InputVars.Drag.Threshold) return;
            m_IsTouching = true;
            delta      = Mathf.Clamp(delta.magnitude, m_InputVars.Drag.InputDeltaRange.x, m_InputVars.Drag.InputDeltaRange.y) * delta.normalized;
            m_InputDelta = Vector2.SmoothDamp(m_InputDelta, delta, ref m_InputSpeedUpVelocity, m_InputVars.Drag.InputSpeedUpDuration);
        }

        private void OnGameEnded()
        {
            m_InputDelta = Vector2.zero;
            m_IsTouching = false;
        }

        private void FixedUpdate()
        {
            if (GameLauncher.GameState != Enums.eGameState.Arena) return;

            if (!m_IsTouching) SlowDownInput();
        }

        private void SlowDownInput()
        {
            m_InputDelta = Vector2.SmoothDamp(m_InputDelta, Vector2.zero, ref m_InputSlowDownVelocity, m_InputVars.Drag.InputSpeedDownDuration);
        }

        public struct NetworkInputData : INetworkInput
        {
            public Vector2 Direction;
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new NetworkInputData
                       {
                           Direction = m_InputDelta
                       };

            input.Set(data);
        }
    }
}