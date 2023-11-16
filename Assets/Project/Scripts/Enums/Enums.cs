namespace Project.Scripts.Enums
{
   public enum eBoostType
   {
      Null,
      Rotate,
      Speed,
      BigHead,
      BulletBody,
   }
   
   public enum eBodyPart
   {
      Head,
      Chest,
      Arm,
      Hand,
      Leg,
      Foot
   }
   
   public enum eGameState
   {
      Lobby,  
      Arena,
      Score,
   }
   
   public enum eFaceSelectState
   {
      Available,
      UnAvailable,
      Selected,
   }
   
   public enum eScreenType
   {
      HUD,
      FaceSelect,
      Score
   }
   public enum ePoolType
   {
      BreakParticle,
      HitParticle,
   }
      
   public enum eAudioType
   {
      Music,
      Hit,
      Detach,
      BoostCollect,
      BoostEnd,
      GameStart,
      GameEnd,
   }
      
   public class eExecutionOrder
   {
      public const int GameInstaller  = -1000;
      public const int StorageManager = -900;
      public const int InputManager   = -800;
      public const int GameManager    = -700;
      public const int LevelManager   = -600;
      public const int CubeManager    = -500;
   }
}