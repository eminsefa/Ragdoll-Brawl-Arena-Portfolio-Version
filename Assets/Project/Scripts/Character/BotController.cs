using Project.Scripts.Essentials;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scripts.Character
{
    public class BotController : CharacterBase
    {
        public static BotController Instance;
        private const string        BOT_SCORE = "BotScore";

        public static int PlayerScore
        {
            get => PlayerPrefs.GetInt(BOT_SCORE, 0);
            set => PlayerPrefs.SetInt(BOT_SCORE, value);
        }

        private Vector2 m_LastPosition;
        private float   m_InputDecideDuration;
        private float   m_InputTimer;
        private Vector2 m_MoveVector;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_InputDecideDuration = GameConfig.Instance.Bot.InputDecideDuration;
            m_InputTimer          = m_InputDecideDuration;
            Instance              = this;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Instance = null;
        }

        private void Update()
        {
            m_InputTimer += Time.deltaTime;
            if (m_InputTimer > m_InputDecideDuration * Random.Range(0.5f, 1f))
            {
                DecideInput();
            }
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (!IsActive) return;

            m_Movement.Move(m_MoveVector, Runner.DeltaTime);
        }

        private void DecideInput()
        {
            var stuck = Vector2.Distance(transform.position, m_LastPosition) < 1.5f;

            m_InputTimer = 0;
            m_MoveVector = stuck ? -m_MoveVector : Random.insideUnitCircle * Random.Range(8, 25);

            m_LastPosition = transform.position;
        }

        public void AddScore()
        {
            PlayerScore++;
        }
    }
}