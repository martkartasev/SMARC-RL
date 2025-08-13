using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Unity.VisualScripting;
using UnityEngine;

namespace BagReplay
{
    public enum ReplayType
    {
        Position,
        Velocity,
        PseudoVelocity,
        Acceleration
    }

    public class BagReplayGhost : MonoBehaviour
    {
        public ArticulationChainComponent body;
        public BagReplay replay;
        public ReplayType type;

        private void Start()
        {
            body.Restart(NED.ConvertToRUF(replay.CurrentBagData.PositionRos), NED.ConvertToRUF(replay.CurrentBagData.OrientationRos));
            body.GetRoot().linearVelocity = NED.ConvertToRUF(replay.CurrentBagData.LinearVelocityRos);
            body.GetRoot().angularVelocity = FRD.ConvertAngularVelocityToRUF(replay.CurrentBagData.AngularVelocityRos);
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
                case ReplayType.Acceleration:
                    DoAccelerationUpdate();
                    break;
            }
        }

        private void DoAccelerationUpdate()
        {
            var newVel = NED.ConvertToRUF(replay.CurrentBagData.LinearVelocityRos);
            var pastVel = NED.ConvertToRUF(replay.PreviousBagData.LinearVelocityRos);
            var newAngVel = FRD.ConvertAngularVelocityToRUF(replay.CurrentBagData.AngularVelocityRos);
            var pastAngVel = FRD.ConvertAngularVelocityToRUF(replay.PreviousBagData.AngularVelocityRos);
            var linearAcc = (newVel - pastVel) / Time.fixedDeltaTime;
            body.GetRoot().AddForce(linearAcc, ForceMode.Acceleration);
            var angularAcc = (newAngVel - pastAngVel) / Time.fixedDeltaTime;
            body.GetRoot().AddTorque(angularAcc, ForceMode.Acceleration);
            Debug.Log(linearAcc + "    " + angularAcc);
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