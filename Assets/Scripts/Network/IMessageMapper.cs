using ExternalCommunication;

namespace Network
{
    public interface IMessageMapper
    {
        Parameters MapReset(ResetParameters resetParameters);
        Action MapAction(ExternalCommunication.Action msg);
        ExternalCommunication.Observation MapObservationToExternal(Observation observation);
    }
}