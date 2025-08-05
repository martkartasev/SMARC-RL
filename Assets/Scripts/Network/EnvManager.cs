using ExternalCommunication;
using UnityEngine;

namespace Network
{
    public abstract class EnvManager : MonoBehaviour
    {
        public abstract void DoRestart(ResetParameters parameters);
        public abstract void DoRestart();
        public abstract Observation BuildObservationMessage();
        public abstract void RecieveAction(Action control);
        public abstract void UpdateSync();
    }
}