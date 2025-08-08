using UnityEngine;

namespace Network
{
    public class GameInit : MonoBehaviour
    {
        public static bool LOADED;
        public CommunicationService network;

        private void Start()
        {
            if (!LOADED)
            {
                Instantiate(network);
                LOADED = true;
            }
        }
    }
}