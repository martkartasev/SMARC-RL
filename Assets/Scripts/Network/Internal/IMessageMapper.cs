using ExternalCommunication;

namespace Network.Internal
{
    public interface IMessageMapper
    {
        Parameters MapReset(ResetParameters resetParameters);
        Action MapAction(ExternalCommunication.Action msg);
        ExternalCommunication.Observation MapObservationToExternal(Observation observation);
    }
}