using Fusion;
using Project.Scripts.Managers;

namespace Project.Scripts.Character
{
    public class PlayerController : CharacterBase
    {
        public override void FixedUpdateNetwork()
        {
            if(!IsActive) return;
            if(!HasStateAuthority) return;
            if (GetInput(out InputManager.NetworkInputData data))
            {
                var moveVector = data.Direction;
                if (moveVector.magnitude < 0.01f) return;

                m_Movement.Move(moveVector, Runner.DeltaTime);
            }
        }
    }
}