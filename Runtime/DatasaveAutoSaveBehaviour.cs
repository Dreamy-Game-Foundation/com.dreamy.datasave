using UnityEngine;

namespace Dreamy.Datasave
{
    public sealed class DatasaveAutoSaveBehaviour : MonoBehaviour
    {
        private IDatasaveService service;

        public void Initialize(IDatasaveService service)
        {
            this.service = service;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                service?.SaveAll();
            }
        }

        private void OnApplicationQuit()
        {
            service?.SaveAll();
        }
    }
}
