using System.Collections.Generic;
using Force;
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

        Hinge yaw, pitch;
        Propeller frontProp, backProp;
        VBS vbs;
        Prismatic lcg;

        private bool hasReset = false;
        private List<ForcePoint> _forcePoints;

        public void Awake()
        {
            yaw = yawHingeGo.GetComponent<Hinge>();
            pitch = pitchHingeGo.GetComponent<Hinge>();
            frontProp = frontPropGo.GetComponent<Propeller>();
            backProp = backPropGo.GetComponent<Propeller>();
            vbs = vbsGo.GetComponent<VBS>();
            lcg = lcgGo.GetComponent<Prismatic>();
            _forcePoints = transform.FindInChildrenRecursive<ForcePoint>();
        }

        public void Initialize(Vector3 position, Quaternion rotation,
            float thrusterHorizontalRad,
            float thrusterVerticalRad,
            float vbsCmd,
            float lcgCmd)
        {
           // Debug.Log(chain.GetRoot().transform.position + "   " + chain.GetRoot().transform.rotation.eulerAngles);
            chain.GetRoot().immovable = true;
            chain.Restart(position, rotation);

            yaw.GetComponentInParent<ArticulationBody>().jointPosition = new ArticulationReducedSpace(thrusterHorizontalRad);
            pitch.GetComponentInParent<ArticulationBody>().jointPosition = new ArticulationReducedSpace(thrusterVerticalRad);
            vbs.GetComponentInParent<ArticulationBody>().jointPosition = new ArticulationReducedSpace(vbs.ComputeTargetValue(vbsCmd));
            lcg.GetComponentInParent<ArticulationBody>().jointPosition = new ArticulationReducedSpace(lcg.ComputeTargetValue(lcgCmd));

            yaw.SetAngle(thrusterHorizontalRad);
            pitch.SetAngle(thrusterVerticalRad);
            vbs.SetPercentage(100 - vbsCmd);
            lcg.SetPercentage(100 - lcgCmd);
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

            yaw.DoUpdate();
            pitch.DoUpdate();
            frontProp.DoUpdate();
            backProp.DoUpdate();
            lcg.DoUpdate();
            vbs.DoUpdate();
        }

        public void ApplyCorrection(Vector3 force, Vector3 torque)
        {
            chain.GetRoot().AddRelativeForce(force);
            chain.GetRoot().AddRelativeTorque(torque);

            yaw.DoUpdate();
            pitch.DoUpdate();
            frontProp.DoUpdate();
            backProp.DoUpdate();
            lcg.DoUpdate();
            vbs.DoUpdate();

            foreach (var forcePoint in _forcePoints)
            {
                forcePoint.DoUpdate();
            }
        }
    }
}