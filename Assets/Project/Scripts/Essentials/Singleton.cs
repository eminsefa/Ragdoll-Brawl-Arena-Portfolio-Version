using UnityEngine;

namespace Project.Scripts.Essentials
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        private static T s_Instance;

        public bool DontDestroyLoad;

        private static bool m_ApplicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = (T) FindObjectOfType(typeof(T));

                    if (s_Instance == null)
                    {
                        if (m_ApplicationIsQuitting && Application.isPlaying)
                        {
                            return s_Instance;
                        }
                        else
                        {
                            var singleton = new GameObject();
                            s_Instance     = singleton.AddComponent<T>();
                            singleton.name = "[Singleton] " + typeof(T);
                        }
                    }
                }

                return s_Instance;
            }
        }

        private void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = gameObject.GetComponent<T>();
                if (DontDestroyLoad)
                {
                    setDontDestroyOnLoad();
                }

                OnAwakeEvent();
            }
            else
            {
                if (this == s_Instance)
                {
                    if (DontDestroyLoad)
                    {
                        setDontDestroyOnLoad();
                    }

                    OnAwakeEvent();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        protected virtual void OnAwakeEvent() { }

        private void setDontDestroyOnLoad()
        {
            DontDestroyLoad = true;
            if (DontDestroyLoad)
            {
                if (transform.parent != null)
                {
                    transform.parent = null;
                }

                DontDestroyOnLoad(gameObject);
            }
        }
    }
}