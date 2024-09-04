using UnityEngine;

namespace AurigaGames.Deep.Core
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (ApplicationManager.IsExiting)
                    {
                        return null;
                    }
                    
                    var singleton = new GameObject();

                    _instance = singleton.AddComponent<T>();
#if UNITY_EDITOR
                    singleton.name = UnityEditor.ObjectNames.NicifyVariableName(typeof(T).Name) + " (singleton)";
#else
                    singleton.name = typeof(T).Name + " (singleton)";
#endif
                    if (_instance.Persistent)
                    {
                        DontDestroyOnLoad(singleton);
                    }
                }

                return _instance;
            }
        }

        protected virtual bool Persistent => true;

        protected void WakeUp() { }
    }
}
