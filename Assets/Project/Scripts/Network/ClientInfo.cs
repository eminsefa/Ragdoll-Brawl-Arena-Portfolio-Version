using UnityEngine;

namespace Project.Scripts.Network
{
    public static class ClientInfo
    {
        private const string FACE_ID = "FaceId";
        public static int FaceId 
        {
            get => PlayerPrefs.GetInt(FACE_ID, -1);
            set => PlayerPrefs.SetInt(FACE_ID, value);
        }
    }
}