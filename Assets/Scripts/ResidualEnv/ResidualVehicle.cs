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
            if (yawHingeGo != null) yaw = yawHingeGo.GetComponent<Hinge>();
            if (pitch != null) pitch = pitchHingeGo.GetComponent<Hinge>();
            if (frontProp != null) frontProp = frontPropGo.GetComponent<Propeller>();
            if (backProp != null) backProp = backPropGo.GetComponent<Propeller>();
            if (vbs != null) vbs = vbsGo.GetComponent<VBS>();
            if (lcg != null) lcg = lcgGo.GetComponent<Prismatic>();
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

            if (yaw != null) yaw.GetComponentInParent<ArticulationBody>().jointPosition = new ArticulationReducedSpace(thrusterHorizontalRad);
            if (pitch != null) pitch.GetComponentInParent<ArticulationBody>().jointPosition = new ArticulationReducedSpace(thrusterVerticalRad);
            if (vbs != null) vbs.GetComponentInParent<ArticulationBody>().jointPosition = new ArticulationReducedSpace(vbs.ComputeTargetValue(vbsCmd));
            if (lcg != null) lcg.GetComponentInParent<ArticulationBody>().jointPosition = new ArticulationReducedSpace(lcg.ComputeTargetValue(lcgCmd));

            yaw?.SetAngle(thrusterHorizontalRad);
            pitch?.SetAngle(thrusterVerticalRad);
            vbs?.SetPercentage(100 - vbsCmd);
            lcg?.SetPercentage(100 - lcgCmd);
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
            chain.GetRoot().linearVelocity = chain.GetRoot().transform.TransformVector(linearVelocity);
            chain.GetRoot().angularVelocity = chain.GetRoot().transform.TransformVector(angularVelocity);

            yaw?.SetAngle(thrusterHorizontalRad);
            pitch?.SetAngle(thrusterVerticalRad);

            vbs?.SetPercentage(vbsCmd);
            lcg?.SetPercentage(lcgCmd);

            frontProp?.SetRpm(thrusterRpm);
            backProp?.SetRpm(thrusterRpm);

            yaw?.DoUpdate();
            pitch?.DoUpdate();
            frontProp?.DoUpdate();
            backProp?.DoUpdate();
            lcg?.DoUpdate();
            vbs?.DoUpdate();
        }

        public void ApplyCorrection(Vector3 force, Vector3 torque)
        {
            chain.GetRoot().AddRelativeForce(force / Time.fixedDeltaTime, ForceMode.Acceleration);
            chain.GetRoot().AddRelativeTorque(torque / Time.fixedDeltaTime, ForceMode.Acceleration);

            yaw?.DoUpdate();
            pitch?.DoUpdate();
            frontProp?.DoUpdate();
            backProp?.DoUpdate();
            lcg?.DoUpdate();
            vbs?.DoUpdate();


            if (_forcePoints != null)
            {
                foreach (var forcePoint in _forcePoints)
                {
                    forcePoint.DoUpdate();
                }
            }
        }
    }
}