using Network;
using UnityEngine;

public class GameInit : MonoBehaviour
{
    public static bool LOADED;
    public CommunicationService network;

    void Start()
    {
        if (!LOADED)
        {
            Instantiate(network);
            LOADED = true;
        }
    }
}