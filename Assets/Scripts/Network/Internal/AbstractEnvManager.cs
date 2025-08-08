using UnityEngine;

namespace Network.Internal
{
    public abstract class AbstractEnvManager : MonoBehaviour
    {
        public abstract void DoRestart(Parameters parameters);
        public abstract Observation BuildObservationMessage();
        public abstract void RecieveAction(Action control);
        public abstract void UpdateSync();
        public abstract IMessageMapper GetMessageMapper();
    }
}