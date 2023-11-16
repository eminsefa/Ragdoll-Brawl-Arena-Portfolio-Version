using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Project.Scripts.Essentials
{
    [CreateAssetMenu(fileName = "GameConfig")]
    public class GameConfig : SingletonScriptableObject<GameConfig>
    {
        public int TargetFrameRate ;
        
        public InputVariables     Input     = new();
        public CharacterVariables Character = new();
        public BotVariables       Bot       = new();
        public BoostVariables     Boost     = new();
        public GravityVariables   Gravity   = new();
        public VisualVariables    Visual    = new();
        public AudioVariables     Audio     = new();
    }

    [Serializable]
    public class InputVariables
    {
        public DragData Drag;

        [Serializable]
        public class DragData
        {
            public float   Threshold;
            public Vector2 InputDeltaRange;
            public float   InputSpeedUpDuration;
            public float   InputSpeedDownDuration;
        }
    }

    [Serializable]
    public class CharacterVariables
    {
        public MovementVariables Movement;
        public FightVariables    Fight;
        public BodyPartVariables BodyPart;
    }

    [Serializable]
    public class MovementVariables
    {
        public float MoveSpeed;
        public float RotateSpeed;

        public float MaxVelocity;
        public float MaxAngularVelocity;
    }

    [Serializable]
    public class FightVariables
    {
        public Vector2 SpeedFactorRange;

        public Vector2 AngularSpeedFactorRange;
        public float   MaxDamage;
        public Vector2 ForceRange;
    }

    [Serializable]
    public class BodyPartVariables
    {
        public BodyDictionary BodyPartHealth;

        public float ImmuneTime;
        public float DamageRate;

        [BoxGroup("Color Variables")] public Color[]  SpriteHealthColorRange;
        [BoxGroup("Color Variables")] public Gradient OutlineColorGradient;

        [BoxGroup("Detach Variables")] public Vector2 DetachForceRange;
        [BoxGroup("Detach Variables")] public float   DetachedScaleDuration;
        [BoxGroup("Detach Variables")] public float   DetachedScaleDelay;
        [BoxGroup("Detach Variables")] public Ease    DetachedScaleEase;
        [BoxGroup("Detach Variables")] public Color   DetachedEndColor;
        [BoxGroup("Detach Variables")] public float   DetachedMaxVelocity;
        [BoxGroup("Detach Variables")] public float   DetachedMaxAngularVelocity;
    }

    [Serializable]
    public class BoostVariables
    {
        public BoostDictionary BoostDictionary;

        [Serializable]
        public struct BoostData
        {
            public float  Duration;
            public string BoostText;
        }
    }

    [Serializable]
    public class GravityVariables
    {
        public Vector2 ChangeDurationRange;
        public Vector2 StrengthRange;
    }

    [Serializable]
    public class VisualVariables
    {
        public Sprite[] HeadSprites;
    }

    [Serializable]
    public class AudioVariables
    {
        public AudioDictionary AudioDictionary;

        [Serializable]
        public struct AudioData
        {
            public AudioClip Clip;
            public Vector2   VolumeRange;
            public Vector2   PitchRange;
        }
    }

    [Serializable]
    public class BotVariables
    {
        public float InputDecideDuration;
    }

    [Serializable] public class BodyDictionary : UnitySerializedDictionary<Enums.eBodyPart, int> { }

    [Serializable] public class BoostDictionary : UnitySerializedDictionary<Enums.eBoostType, BoostVariables.BoostData> { }

    [Serializable] public class AudioDictionary : UnitySerializedDictionary<Enums.eAudioType, AudioVariables.AudioData> { }
}