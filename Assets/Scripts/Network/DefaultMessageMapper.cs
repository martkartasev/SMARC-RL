using System.Linq;
using ExternalCommunication;
using Action = Network.Action;
using Observation = Network.Observation;

namespace Network
{
    public class DefaultMapper : IMessageMapper
    {
        public Parameters MapReset(ResetParameters resetParameters)
        {
            return new Parameters();
        }

        public Action MapAction(ExternalCommunication.Action msg)
        {
            return new Action
            {
                Continuous = msg.Continuous.ToArray(),
                Discrete = msg.Discrete.ToArray()
            };
        }

        public ExternalCommunication.Observation MapObservationToExternal(Observation observation)
        {
            var mapObservationToExternal = new ExternalCommunication.Observation();
            mapObservationToExternal.Floats.AddRange(observation.Continuous);
            mapObservationToExternal.Ints.AddRange(observation.Discrete);
            return mapObservationToExternal;
        }
    }
}