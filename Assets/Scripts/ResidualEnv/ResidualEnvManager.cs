using Network;
using Swan;
using UnityEngine;
using Action = Network.Action;
using Observation = Network.Observation;

namespace ResidualEnv
{
    public class ResidualAbstractEnvManager : AbstractEnvManager
    {
        public ResidualVehicle Vehicle;

        private DefaultMapper _messageMapper;
        private Action _latestAction;

        private void OnEnable()
        {
            _messageMapper = new DefaultMapper();
        }

        public override Observation BuildObservationMessage()
        {
            var observations = new float[10];
            
            observations[0] = Vehicle.chain.GetRoot().transform.rotation.x;
            observations[1] = Vehicle.chain.GetRoot().transform.rotation.y;
            observations[2] = Vehicle.chain.GetRoot().transform.rotation.z;
            observations[3] = Vehicle.chain.GetRoot().transform.rotation.w;
            
            observations[4] = Vehicle.chain.GetRoot().linearVelocity.x;
            observations[5] = Vehicle.chain.GetRoot().linearVelocity.y;
            observations[6] = Vehicle.chain.GetRoot().linearVelocity.z;
            
            observations[7] = Vehicle.chain.GetRoot().angularVelocity.x;
            observations[8] = Vehicle.chain.GetRoot().angularVelocity.y;
            observations[9] = Vehicle.chain.GetRoot().angularVelocity.z;
            
            return new Observation
            {
                Continuous = observations
            };
        }

        public override void DoRestart(Parameters parameters)
        {
            Vehicle.Initialize(transform.TransformPoint(Vector3.zero), new Quaternion(parameters.Continuous[0], parameters.Continuous[1], parameters.Continuous[2], parameters.Continuous[3]));
        }

        public override void RecieveAction(Action action)
        {
            _latestAction = action;
            Vehicle.SetAction(
                new Vector3(action.Continuous[0], action.Continuous[1], action.Continuous[2]),
                new Vector3(action.Continuous[3], action.Continuous[4], action.Continuous[5]),
                action.Continuous[6],
                action.Continuous[7],
                action.Continuous[8],
                action.Continuous[9],
                action.Continuous[10]);
        }

        public override void UpdateSync()
        {
            Vehicle.ApplyCorrectiveForce(
                new Vector3(_latestAction.Continuous[11], _latestAction.Continuous[12], _latestAction.Continuous[13]),
                new Vector3(_latestAction.Continuous[14], _latestAction.Continuous[15], _latestAction.Continuous[16])
            );
        }

        public override IMessageMapper GetMessageMapper()
        {
            return _messageMapper;
        }
    }
}