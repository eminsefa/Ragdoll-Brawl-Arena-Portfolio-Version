using UnityEngine;

namespace Project.Scripts.Essentials
{
    public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
    {
        static T s_Instance = null;

        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    T[] objects = Resources.FindObjectsOfTypeAll<T>();

                    if (objects == null || objects.Length == 0)
                    {
                        objects = Resources.LoadAll<T>(string.Empty);
                    }

                    if (objects.Length > 0 && objects[0] != null)
                    {
                        s_Instance = objects[0];
                    }
                }

                return s_Instance;
            }
        }
    }
}
