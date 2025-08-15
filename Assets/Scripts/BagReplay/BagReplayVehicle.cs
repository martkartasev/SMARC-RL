using Unity.InferenceEngine;
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

        public ModelAsset modelAsset;
        private Model runtimeModel;
        private Worker worker;

        Hinge yaw, pitch;
        Propeller frontProp, backProp;
        VBS vbs;
        Prismatic lcg;

        private bool hasReset = false;

        void Start()
        {
            if (yawHingeGo != null) yaw = yawHingeGo.GetComponent<Hinge>();
            if (pitch != null) pitch = pitchHingeGo.GetComponent<Hinge>();
            if (frontProp != null) frontProp = frontPropGo.GetComponent<Propeller>();
            if (backProp != null) backProp = backPropGo.GetComponent<Propeller>();
            if (vbs != null) vbs = vbsGo.GetComponent<VBS>();
            if (lcg != null) lcg = lcgGo.GetComponent<Prismatic>();
            chain.Restart(NED.ConvertToRUF(replay.CurrentBagData.PositionRos), NED.ConvertToRUF(replay.CurrentBagData.OrientationRos));

            if (modelAsset != null)
            {
                runtimeModel = ModelLoader.Load(modelAsset);
                worker = new Worker(runtimeModel, BackendType.CPU);
            }
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

            yaw?.SetAngle(replay.CurrentBagData.ThrusterHorizontalRad);
            pitch?.SetAngle(replay.CurrentBagData.ThrusterVerticalRad);

            vbs?.SetPercentage(replay.CurrentBagData.Vbs);
            lcg?.SetPercentage(replay.CurrentBagData.Lcg);

            frontProp?.SetRpm(replay.CurrentBagData.Thruster1RPM);
            backProp?.SetRpm(replay.CurrentBagData.Thruster2RPM);

            if (runtimeModel != null) AddResidualAcceleration();
        }

        private void AddResidualAcceleration()
        {
            var linearVelocity = chain.GetRoot().transform.InverseTransformVector(chain.GetRoot().linearVelocity);
            var angularVelocity = chain.GetRoot().transform.InverseTransformVector(chain.GetRoot().angularVelocity);
            float[] data = new float[]
            {
                linearVelocity.x,
                linearVelocity.y,
                linearVelocity.z,
                angularVelocity.x / 7f,
                angularVelocity.y / 7f,
                angularVelocity.z / 7f,
                replay.CurrentBagData.ThrusterHorizontalRad / 0.13f,
                replay.CurrentBagData.ThrusterVerticalRad / 0.13f,
                replay.CurrentBagData.Vbs / 100,
                replay.CurrentBagData.Lcg / 100,
                replay.CurrentBagData.Thruster1RPM / 1000f
            };

            // Create a 3D tensor shape with size 3 × 1 × 3
            TensorShape shape = new TensorShape(1, 11);

            // Create a new tensor from the array
            Tensor<float> tensor = new Tensor<float>(shape, data);
            worker.Schedule(tensor);
            Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
            var result = outputTensor.ReadbackAndClone();

            chain.GetRoot().AddRelativeForce(new Vector3(result[0], result[1], result[2]) / Time.fixedDeltaTime, ForceMode.Acceleration);
            chain.GetRoot().AddRelativeTorque(new Vector3(result[3] * 7, result[4] * 7, result[5] * 7) / Time.fixedDeltaTime, ForceMode.Acceleration);
            Debug.Log("Residual:" + chain.GetRoot().linearVelocity);
        }
    }
}