using System;
using Network;
using Network.Mapper;
using Network.Message;
using UnityEngine;
using Action = Network.Message.Action;
using Observation = Network.Message.Observation;

namespace ResidualEnv
{
    public class ResidualAbstractEnvManager : AbstractEnvManager
    {
        public ResidualVehicle vehicle;

        private DefaultMapper _messageMapper;
        private Action _latestAction;

        private void Awake()
        {
            _messageMapper = new DefaultMapper();
        }

        public override void DoRestart(Parameters parameters)
        {
            var orientation = new Quaternion(parameters.Continuous[0], parameters.Continuous[1], parameters.Continuous[2], parameters.Continuous[3]);
            var thrusterHorizontalRad = parameters.Continuous[4] * 0.13f;
            var thrusterVerticalRad = parameters.Continuous[5] * 0.13f;
            var vbs = parameters.Continuous[6] * 100;
            var lcg = parameters.Continuous[7] * 100;

            vehicle.Initialize(transform.TransformPoint(Vector3.zero),
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
            var angularVelocity = new Vector3(action.Continuous[3] * 5, action.Continuous[4] * 5, action.Continuous[5] * 5);
            var thrusterHorizontalRad = action.Continuous[6] * 0.13f;
            var thrusterVerticalRad = action.Continuous[7] * 0.13f;
            var vbs = action.Continuous[8] * 100;
            var lcg = action.Continuous[9] * 100;
            var thrusterRPM = action.Continuous[10] * 1000;

            vehicle.SetAction(
                linearVelocity,
                angularVelocity,
                thrusterHorizontalRad,
                thrusterVerticalRad,
                vbs,
                lcg,
                thrusterRPM);
        }

        public override Observation BuildObservationMessage()
        {
            var observations = new float[10];

            observations[0] = vehicle.chain.GetRoot().transform.rotation.x;
            observations[1] = vehicle.chain.GetRoot().transform.rotation.y;
            observations[2] = vehicle.chain.GetRoot().transform.rotation.z;
            observations[3] = vehicle.chain.GetRoot().transform.rotation.w;

            observations[4] = vehicle.chain.GetRoot().linearVelocity.x;
            observations[5] = vehicle.chain.GetRoot().linearVelocity.y;
            observations[6] = vehicle.chain.GetRoot().linearVelocity.z;

            observations[7] = vehicle.chain.GetRoot().angularVelocity.x;
            observations[8] = vehicle.chain.GetRoot().angularVelocity.y;
            observations[9] = vehicle.chain.GetRoot().angularVelocity.z;

            return new Observation
            {
                Continuous = observations,
                Discrete = Array.Empty<int>()
            };
        }


        public override void FixedUpdateManual()
        {
            var correctiveForce = new Vector3(_latestAction.Continuous[11], _latestAction.Continuous[12], _latestAction.Continuous[13]);
            var correctiveTorque = new Vector3(_latestAction.Continuous[14], _latestAction.Continuous[15], _latestAction.Continuous[16]);

            vehicle.ApplyCorrection(correctiveForce, correctiveTorque);
        }

        public override IMessageMapper GetMessageMapper()
        {
            return _messageMapper;
        }
    }
}