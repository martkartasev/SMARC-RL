using System;
using DefaultNamespace;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

namespace Inputs
{
    public enum ReplayType
    {
        Position,
        Velocity,
        PseudoVelocity,
    }

    public class BagReplayGhost : MonoBehaviour
    {
        public ArticulationChainComponent body;
        public BagReplay replay;
        public ReplayType type;

        private void Start()
        {
            body.Restart(NED.ConvertToRUF(replay.positionROS), NED.ConvertToRUF(replay.orientationROS));
        }

        private void FixedUpdate()
        {
            switch (type)
            {
                case ReplayType.Position:
                    DoPositionUpdate();
                    break;
                case ReplayType.Velocity:
                    DoVelocityUpdate();
                    break;
                case ReplayType.PseudoVelocity:
                    DoPseudoVelocityUpdate();
                    break;
            }
        }

        private void DoVelocityUpdate()
        {
            body.GetRoot().linearVelocity = NED.ConvertToRUF(replay.linearVelocityROS);
            body.GetRoot().angularVelocity = FRD.ConvertAngularVelocityToRUF(replay.angularVelocityROS);
        }

        private void DoPseudoVelocityUpdate()
        {
            body.GetRoot().linearVelocity = NED.ConvertToRUF((replay.positionROS - replay.prev_positionROS) / Time.fixedDeltaTime);
            body.GetRoot().angularVelocity = GetAngularVelocity(NED.ConvertToRUF(replay.prev_orientationROS), NED.ConvertToRUF(replay.orientationROS));
        }

        private void DoPositionUpdate()
        {
            if (replay.positionROS != Vector3.zero)
            {
                body.GetRoot().TeleportRoot(NED.ConvertToRUF(replay.positionROS), NED.ConvertToRUF(replay.orientationROS));
            }
        }

        Vector3 GetAngularVelocity(Quaternion previousRotation, Quaternion currentRotation)
        {
            Quaternion deltaRotation = currentRotation * Quaternion.Inverse(previousRotation);

            float angle;
            Vector3 axis;
            deltaRotation.ToAngleAxis(out angle, out axis);

            // Ensure the angle is in radians and in correct direction
            if (angle > 180f)
                angle -= 360f;

            // Convert angle from degrees to radians
            angle *= Mathf.Deg2Rad;

            // Angular velocity vector
            return axis * (angle / Time.fixedDeltaTime);
        }
    }
}