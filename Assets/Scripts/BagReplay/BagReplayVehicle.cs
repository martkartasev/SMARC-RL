using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using VehicleComponents.Actuators;

namespace BagReplay
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
            chain.Restart(NED.ConvertToRUF(replay.CurrentBagData.PositionRos), NED.ConvertToRUF(replay.CurrentBagData.OrientationRos));
        }

        private void FixedUpdate()
        {
            if (hasReset && chain.GetRoot().immovable)
            {
                chain.GetRoot().immovable = false;
                chain.GetRoot().linearVelocity = NED.ConvertToRUF(replay.CurrentBagData.LinearVelocityRos);
                chain.GetRoot().angularVelocity = FRD.ConvertAngularVelocityToRUF(replay.CurrentBagData.AngularVelocityRos);
            }

            if (!hasReset)
            {
                hasReset = true;

                chain.GetRoot().immovable = true;
            }
   
            yaw.SetAngle(replay.CurrentBagData.ThrusterHorizontalRad);
            pitch.SetAngle(replay.CurrentBagData.ThrusterVerticalRad);

            vbs.SetPercentage(100 - replay.CurrentBagData.Vbs);
            lcg.SetPercentage(100 - replay.CurrentBagData.Lcg);

            frontProp.SetRpm(replay.CurrentBagData.Thruster1RPM);
            backProp.SetRpm(replay.CurrentBagData.Thruster2RPM);
        }
    }
}