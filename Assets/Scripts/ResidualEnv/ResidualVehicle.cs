using DefaultNamespace;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Unity.VisualScripting;
using UnityEngine;
using VehicleComponents.Actuators;

namespace ResidualEnv
{
    public class ResidualVehicle
    {
        public ArticulationBody yawHingeGo;
        public ArticulationBody pitchHingeGo;
        public ArticulationBody frontPropGo;
        public ArticulationBody backPropGo;
        public ArticulationBody vbsGo;
        public ArticulationBody lcgGo;

        public ArticulationChainComponent chain;

        private bool hasReset = false;

        public void Initialize(Vector3 position, Quaternion rotation)
        {
            chain.GetRoot().immovable = true;
            chain.Restart(position, rotation);
        }

        public void SetAction(
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            float thrusterHorizontalRad,
            float thrusterVerticalRad,
            float vbs,
            float lcg,
            float thrusterRpm)
        {
            chain.GetRoot().immovable = false;

            chain.GetRoot().linearVelocity = linearVelocity;
            chain.GetRoot().angularVelocity = angularVelocity;

            yaw.SetAngle(thrusterHorizontalRad);
            pitch.SetAngle(thrusterVerticalRad);

            vbsA.SetPercentage(100 - vbs);
            lcgA.SetPercentage(100 - lcg);

            frontProp.SetRpm(thrusterRpm);
            backProp.SetRpm(thrusterRpm);
        }

        public void ApplyCorrectiveForce(Vector3 force, Vector3 torque)
        {
            chain.GetRoot().AddRelativeForce(force);
            chain.GetRoot().AddRelativeTorque(torque);
        }
    }
}