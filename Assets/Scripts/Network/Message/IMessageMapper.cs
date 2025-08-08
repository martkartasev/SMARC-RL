using ExternalCommunication;
using Network.Message;
using Action = Network.Message.Action;
using Observation = Network.Message.Observation;

namespace Network.Mapper
{
    public interface IMessageMapper
    {
        Parameters MapReset(ResetParameters resetParameters);
        Action MapAction(ExternalCommunication.Action msg);
        ExternalCommunication.Observation MapObservationToExternal(Observation observation);
    }
}