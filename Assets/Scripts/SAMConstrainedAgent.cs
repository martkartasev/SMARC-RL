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
            if (odometry.useNED)
            {
                sensor.AddObservation(new Vector3((float) (pose.position.x / initMax.x), (float) (pose.position.y / initMax.y), (float) (pose.position.z / initMax.z)).To<NED>().ToUnityVec3());
            }
            else
            {
                sensor.AddObservation(new Vector3((float) (pose.position.x / initMax.x), (float) (pose.position.y / initMax.y), (float) (pose.position.z / initMax.z)).To<ENU>().ToUnityVec3());
            }
        }


        protected override void InitializeTarget()
        {
            if (randomizeSpeed) targetSpeed = Random.Range(0.1f, 0.5f);
            targetObject.localPosition = new Vector3(Random.Range(initMin.x, initMax.x), Random.Range(initMin.y, initMax.y), Random.Range(initMin.z, initMax.z));
            targetObject.localRotation = Quaternion.Euler(new Vector3(Random.Range(-15, 15), Random.Range(0, 360), 0));
        }

        protected override float ComputeReward()
        {
            var reward = base.ComputeReward();
            reward += 0.5f * Mathf.Min(collisionPool.collisionReward + collisionGlass.collisionReward, -1) / MaxStep; //Collisions return -1 if colliding, if both collide, only apply a total of -1;
            return reward;
        }

        protected override float VelocityReward()
        {
            return GetMatchingVelocityReward(body.transform.forward * targetSpeed, body.linearVelocity); // Pool is small, not using "look at" component;
        }
    }
}