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
            maxDistance = 5f;
            initMax = new(2.7f, 2, 1.5f);
            initMin = new(-2.7f, 0.2f, -1.5f);
        }
        

        protected override void InitializeTarget()
        {
            if (randomizeSpeed) targetSpeed = Random.Range(0.1f, 0.5f);
            targetObject.localPosition = new Vector3(Random.Range(-2.8f, 2.8f), Random.Range(-0, 2.35f), Random.Range(-1.4f, 1.4f));
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