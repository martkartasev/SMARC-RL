using System;

namespace Reward
{
    public class PotentialReward : IRewardFunction
    {
        private float previous = -1;
        private float _multiplier;
        private readonly Func<float> _metric;
        private readonly float _max;


        public PotentialReward(Func<float> metricFunction, float max, float multiplier = 1f)
        {
            _max = max;
            _multiplier = multiplier;
            _metric = metricFunction;
        }

        private void Initialize()
        {
            previous = (_max - _metric.Invoke()) / _max;
        }

        public float Compute()
        {
            if (previous < 0.0f) Initialize(); //Lazy init because sometimes the reset is not immediate.

            var current = (_max - _metric.Invoke()) / _max;

            var compute = previous - current;
            previous = current;

            return Math.Clamp(compute * _multiplier, -1, 1);
        }
    }
}