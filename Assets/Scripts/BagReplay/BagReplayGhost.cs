using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

namespace BagReplay
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
            body.Restart(NED.ConvertToRUF(replay.CurrentBagData.PositionRos), NED.ConvertToRUF(replay.CurrentBagData.OrientationRos));
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
            body.GetRoot().linearVelocity = NED.ConvertToRUF(replay.CurrentBagData.LinearVelocityRos);
            body.GetRoot().angularVelocity = FRD.ConvertAngularVelocityToRUF(replay.CurrentBagData.AngularVelocityRos);
        }

        private void DoPseudoVelocityUpdate()
        {
            body.GetRoot().linearVelocity = NED.ConvertToRUF((replay.CurrentBagData.PositionRos - replay.CurrentBagData.PrevPositionRos) / Time.fixedDeltaTime);
            body.GetRoot().angularVelocity = GetAngularVelocity(NED.ConvertToRUF(replay.CurrentBagData.PrevOrientationRos), NED.ConvertToRUF(replay.CurrentBagData.OrientationRos));
        }

        private void DoPositionUpdate()
        {
            if (replay.CurrentBagData.PositionRos != Vector3.zero)
            {
                body.GetRoot().TeleportRoot(NED.ConvertToRUF(replay.CurrentBagData.PositionRos), NED.ConvertToRUF(replay.CurrentBagData.OrientationRos));
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