using UnityEngine;

namespace Project.Scripts.UI
{
    public class ScreenBase : MonoBehaviour
    {
        public void Open()
        {
            gameObject.SetActive(true);
        }
    
        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
