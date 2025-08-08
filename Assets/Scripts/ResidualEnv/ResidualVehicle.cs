using DefaultNamespace;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Unity.VisualScripting;
using UnityEngine;
using VehicleComponents.Actuators;

namespace ResidualEnv
{
    public class ResidualVehicle : MonoBehaviour
    {
        public GameObject yawHingeGo;
        public GameObject pitchHingeGo;
        public GameObject frontPropGo;
        public GameObject backPropGo;
        public GameObject vbsGo;
        public GameObject lcgGo;

        public ArticulationChainComponent chain;
        public BagReplay replay;

        Hinge yaw, pitch;
        Propeller frontProp, backProp;
        VBS vbs;
        Prismatic lcg;

        private bool hasReset = false;

        void OnEnable()
        {
            yaw = yawHingeGo.GetComponent<Hinge>();
            pitch = pitchHingeGo.GetComponent<Hinge>();
            frontProp = frontPropGo.GetComponent<Propeller>();
            backProp = backPropGo.GetComponent<Propeller>();
            vbs = vbsGo.GetComponent<VBS>();
            lcg = lcgGo.GetComponent<Prismatic>();
        }

        public void Initialize(Vector3 position, Quaternion rotation,
            float thrusterHorizontalRad,
            float thrusterVerticalRad,
            float vbsCmd,
            float lcgCmd)
        {
            chain.GetRoot().immovable = true;
            chain.Restart(position, rotation);

            yaw.GetComponent<ArticulationBody>().jointPosition = new ArticulationReducedSpace(thrusterHorizontalRad);
            pitch.GetComponent<ArticulationBody>().jointPosition = new ArticulationReducedSpace(thrusterVerticalRad);
            vbs.GetComponent<ArticulationBody>().jointPosition = new ArticulationReducedSpace(vbsCmd);
            lcg.GetComponent<ArticulationBody>().jointPosition = new ArticulationReducedSpace(lcgCmd);
        }


        public void SetAction(
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            float thrusterHorizontalRad,
            float thrusterVerticalRad,
            float vbsCmd,
            float lcgCmd,
            float thrusterRpm)
        {
            chain.GetRoot().immovable = false;

            chain.GetRoot().linearVelocity = linearVelocity;
            chain.GetRoot().angularVelocity = angularVelocity;

            yaw.SetAngle(thrusterHorizontalRad);
            pitch.SetAngle(thrusterVerticalRad);

            vbs.SetPercentage(100 - vbsCmd);
            lcg.SetPercentage(100 - lcgCmd);

            frontProp.SetRpm(thrusterRpm);
            backProp.SetRpm(thrusterRpm);
        }

        public void ApplyCorrection(Vector3 force, Vector3 torque)
        {
            chain.GetRoot().AddRelativeForce(force);
            chain.GetRoot().AddRelativeTorque(torque);

            yaw.DoUpdate();
            pitch.DoUpdate();
            lcg.DoUpdate();
            vbs.DoUpdate();
            frontProp.DoUpdate();
            backProp.DoUpdate();
        }
    }
}