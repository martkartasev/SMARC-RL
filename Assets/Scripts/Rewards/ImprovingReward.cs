using System;

namespace Rewards
{
    public class ImprovingReward : IRewardFunction
    {
        private float initial;
        private float lowest;
        private readonly Func<float> reward;
        private readonly float modifier;

        public ImprovingReward(Func<float> rewardFunction, float lowestInitialMod = 1f)
        {
            modifier = lowestInitialMod;
            reward = rewardFunction;
        }

        private void Initialize()
        {
            initial = reward.Invoke() * modifier;
            lowest = reward.Invoke() * modifier;
        }

        public float Compute()
        {
            if (initial == 0.0f) Initialize(); //Lazy init because sometimes the reset is not immediate.
            var current = reward.Invoke();
            if (current < lowest)
            {
                var compute = (lowest - current) / initial;
                lowest = current;
                return compute;
            }

            return 0.0f;
        }
    }
}