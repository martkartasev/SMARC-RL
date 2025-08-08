using Network;
using Action = Network.Action;
using Observation = Network.Observation;

namespace ResidualEnv
{
    public class ResidualAbstractEnvManager : AbstractEnvManager
    {
        public ResidualVehicle Vehicle;

        private DefaultMapper _messageMapper;

        private void OnEnable()
        {
            _messageMapper = new DefaultMapper();
        }

        public override Observation BuildObservationMessage()
        {
            var observations = new float[16];
            return new Observation
            {
                Continuous = observations
            };
        }

        public override void DoRestart(Parameters parameters)
        {
        }

        public override void RecieveAction(Action control)
        {
            throw new System.NotImplementedException();
        }

        public override void UpdateSync()
        {
            throw new System.NotImplementedException();
        }

        public override IMessageMapper GetMessageMapper()
        {
            return _messageMapper;
        }
    }
}