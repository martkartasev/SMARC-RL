using Unity.MLAgents.Sensors;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Unity.VisualScripting;
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

            var absoultePositionNorm = new Vector3(body.transform.localPosition.x / initMax.x, body.transform.localPosition.y / initMax.y, body.transform.localPosition.z / initMax.z).To<ENU>().ToUnityVec3().ForceNormalizeVector();

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
            var distancePenalty = _distancePenalty.Compute();
            var reward = 0.5f * distancePenalty / MaxStep;

            var alignmentPenalty = -(1 - Mathf.Abs(Quaternion.Dot(targetObject.rotation, body.transform.rotation)));
            var alignmentPenaltyProximity = distancePenalty > -0.2f ? alignmentPenalty : -1f;
            reward += 0.25f * alignmentPenaltyProximity / MaxStep;
            
            var collisionPenalty = Mathf.Clamp(collisionPool.collisionPenalty + collisionGlass.collisionPenalty, -1, 0);
            reward += 0.25f * collisionPenalty / MaxStep;
            
            Debug.Log("distance:" +distancePenalty + " align:" + alignmentPenaltyProximity + " collision:" +collisionPenalty);
            
            return reward;
        }

        protected override float VelocityReward()
        {
            return GetMatchingVelocityReward(body.transform.forward * targetSpeed, body.linearVelocity); // Pool is small, not using "look at" component;
        }
    }
}