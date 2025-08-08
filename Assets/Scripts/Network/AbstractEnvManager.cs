using Network.Mapper;
using Network.Message;
using UnityEngine;

namespace Network
{
    public abstract class AbstractEnvManager : MonoBehaviour
    {
        public abstract void DoRestart(Parameters parameters);
        public abstract Observation BuildObservationMessage();
        public abstract void ReceiveAction(Action control);
        public abstract void FixedUpdateManual();
        public abstract IMessageMapper GetMessageMapper();
    }
}