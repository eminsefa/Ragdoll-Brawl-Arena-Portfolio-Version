using System;
using Project.Scripts.Network;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Scripts.InputProject
{
    public class InputButton : Button, IDragHandler
    {
        private Vector2 m_InputPos;
        private bool    m_IsTouching;

        public static event Action          OnInputDown;
        public static event Action          OnInputUp;
        public static event Action<Vector2> OnInputDelta;

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            OnInputDown?.Invoke();
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            m_IsTouching = false;
            OnInputUp?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(GameLauncher.GameState !=Enums.eGameState.Arena) return;

            var inputPos = eventData.position;
            if (!m_IsTouching)
            {
                m_InputPos   = inputPos;
                m_IsTouching = true;
                return;
            }

            var delta = inputPos - m_InputPos;
            OnInputDelta?.Invoke(delta);

            m_InputPos = inputPos;
        }
    }
#if UNITY_EDITOR
//To simplify button
    [UnityEditor.CustomEditor(typeof(InputButton))]
    public class MenuButtonEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            InputButton targetMenuButton = (InputButton) target;
        }
    }
#endif
}