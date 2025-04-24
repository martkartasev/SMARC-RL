using System;

namespace Reward
{
    public class DistanceReward : IRewardFunction
    {
        private float initial;
        private float lowest;
        private readonly Func<float> reward;
        private readonly float maxDistance;

        public DistanceReward(Func<float> rewardFunction, float maxDistance = 25f)
        {
            this.maxDistance = maxDistance;
            reward = rewardFunction;
        }


        public float Compute()
        {
            var current = reward.Invoke();

            return Math.Clamp((maxDistance - current) / maxDistance, 0, 1);
        }
    }
}