using System;

namespace Rewards
{
    public class DistancePenalty : IRewardFunction
    {
        private float initial;
        private float lowest;
        private readonly Func<float> reward;
        private readonly float maxDistance;

        public DistancePenalty(Func<float> rewardFunction, float maxDistance = 25f)
        {
            this.maxDistance = maxDistance;
            reward = rewardFunction;
        }


        public float Compute()
        {
            var current = reward.Invoke();

            return Math.Clamp(-current / maxDistance, -1, 0);
        }
    }
}