using DefaultNamespace;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using VehicleComponents.Actuators;

namespace Inputs
{
    public class BagReplayVehicle : MonoBehaviour
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

        void Start()
        {
            yaw = yawHingeGo.GetComponent<Hinge>();
            pitch = pitchHingeGo.GetComponent<Hinge>();
            frontProp = frontPropGo.GetComponent<Propeller>();
            backProp = backPropGo.GetComponent<Propeller>();
            vbs = vbsGo.GetComponent<VBS>();
            lcg = lcgGo.GetComponent<Prismatic>();
            chain.Restart(NED.ConvertToRUF(replay.positionROS), NED.ConvertToRUF(replay.orientationROS));
        }

        private void FixedUpdate()
        {
            if (hasReset && chain.GetRoot().immovable)
            {
                chain.GetRoot().immovable = false;
                chain.GetRoot().linearVelocity = NED.ConvertToRUF(replay.linearVelocityROS);
                chain.GetRoot().angularVelocity = FRD.ConvertAngularVelocityToRUF(replay.angularVelocityROS);
            }

            if (!hasReset)
            {
                hasReset = true;

                chain.GetRoot().immovable = true;
            }
            Debug.Log(NED.ConvertToRUF(replay.linearVelocityROS) - chain.GetRoot().linearVelocity);
            Debug.Log(FRD.ConvertAngularVelocityToRUF(replay.angularVelocityROS) - chain.GetRoot().angularVelocity);

            yaw.SetAngle(replay.thrusterHorizontalRad);
            pitch.SetAngle(replay.thrusterVerticalRad);

            vbs.SetPercentage(100 - replay.vbs);
            lcg.SetPercentage(100 - replay.lcg);

            frontProp.SetRpm(replay.thruster1rpm);
            backProp.SetRpm(replay.thruster2rpm);
        }
    }
}