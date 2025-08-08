using Network;
using Network.Mapper;
using Network.Message;
using Swan;
using UnityEngine;
using Action = Network.Message.Action;
using Observation = Network.Message.Observation;

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
            var orientation = new Quaternion(parameters.Continuous[0], parameters.Continuous[1], parameters.Continuous[2], parameters.Continuous[3]);
            var thrusterHorizontalRad = parameters.Continuous[4];
            var thrusterVerticalRad = parameters.Continuous[5];
            var vbs = parameters.Continuous[6];
            var lcg = parameters.Continuous[7];
            Vehicle.Initialize(
                transform.TransformPoint(Vector3.zero),
                orientation,
                thrusterHorizontalRad,
                thrusterVerticalRad,
                vbs,
                lcg);
        }

        public override void ReceiveAction(Action action)
        {
            _latestAction = action;

            var linearVelocity = new Vector3(action.Continuous[0], action.Continuous[1], action.Continuous[2]);
            var angularVelocity = new Vector3(action.Continuous[3], action.Continuous[4], action.Continuous[5]);
            var thrusterHorizontalRad = action.Continuous[6];
            var thrusterVerticalRad = action.Continuous[7];
            var vbs = action.Continuous[8];
            var lcg = action.Continuous[9];
            var thrusterRPM = action.Continuous[10];

            Vehicle.SetAction(
                linearVelocity,
                angularVelocity,
                thrusterHorizontalRad,
                thrusterVerticalRad,
                vbs,
                lcg,
                thrusterRPM);
        }

        public override void FixedUpdateManual()
        {
            var correctiveForce = new Vector3(_latestAction.Continuous[11], _latestAction.Continuous[12], _latestAction.Continuous[13]);
            var correctiveTorque = new Vector3(_latestAction.Continuous[14], _latestAction.Continuous[15], _latestAction.Continuous[16]);
            Vehicle.ApplyCorrection(correctiveForce, correctiveTorque);
        }

        public override IMessageMapper GetMessageMapper()
        {
            return _messageMapper;
        }
    }
}