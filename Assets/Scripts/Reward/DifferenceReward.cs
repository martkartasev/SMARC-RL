using System;

namespace Reward
{
    public class DifferenceReward : IRewardFunction
    {
        private float previous = -1;
        private readonly Func<float> reward;
        private readonly float rewardMultiplier;

        public DifferenceReward(Func<float> rewardFunction, float multiplier = 1f)
        {
            rewardMultiplier = multiplier;
            reward = rewardFunction;
        }

        private void Initialize()
        {
            previous = reward.Invoke();
        }

        public float Compute()
        {
            if (previous < 0.0f) Initialize(); //Lazy init because sometimes the reset is not immediate.
            
            var current = reward.Invoke();
            var compute = previous - current;
            if (compute < 0) compute *= 2; // Negative larger than positive
            previous = current;
            return Math.Clamp(compute * rewardMultiplier, -1, 1);
        }
    }
}