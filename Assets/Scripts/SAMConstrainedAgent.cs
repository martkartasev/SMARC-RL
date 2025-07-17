using Unity.MLAgents.Sensors;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

namespace DefaultNamespace
{
    //Simple extension of the basic movement class, mainly observation, reward and initialization differences
    public class SAMConstrainedAgent : SAMGeneralMovementLearningAgent
    {
        public CollisionRewarder collisionPool;
        public CollisionRewarder collisionGlass;

        public new void Awake()
        {
            base.Awake();
            maxDistance = 9f;
            initMax = new(3.7f, 2.6f, 1.5f);
            initMin = new(-3.7f, 0.2f, -1.5f);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            base.CollectObservations(sensor);
            var pose = odometry.GetRosMsg().pose.pose;

            var absoultePositionNorm = new Vector3((float)(pose.position.x / initMax.x), (float)(pose.position.y / initMax.y), (float)(pose.position.z / initMax.z));
            absoultePositionNorm = odometry.useNED ? absoultePositionNorm.To<NED>().ToUnityVec3() : absoultePositionNorm.To<ENU>().ToUnityVec3();

            sensor.AddObservation(absoultePositionNorm);
        }


        protected override void InitializeTarget()
        {
            if (randomizeSpeed) targetSpeed = Random.Range(0.1f, 0.5f);
            targetObject.localPosition = new Vector3(Random.Range(initMin.x, initMax.x), Random.Range(initMin.y, initMax.y), Random.Range(initMin.z, initMax.z));
            targetObject.localRotation = Quaternion.Euler(new Vector3(Random.Range(-15, 15), Random.Range(0, 360), 0));
        }

        protected override float ComputeReward()
        {
            var targetAlignmentPenalty = -Mathf.Abs(Quaternion.Dot(targetObject.rotation, body.transform.rotation));

            var reward = 0.5f * _distancePenalty.Compute() / MaxStep;
            reward += 0.25f * targetAlignmentPenalty / MaxStep;
            reward += 0.25f * Mathf.Min(collisionPool.collisionPenalty + collisionGlass.collisionPenalty, -1) / MaxStep;
            return reward;
        }

        protected override float VelocityReward()
        {
            return GetMatchingVelocityReward(body.transform.forward * targetSpeed, body.linearVelocity); // Pool is small, not using "look at" component;
        }
    }
}