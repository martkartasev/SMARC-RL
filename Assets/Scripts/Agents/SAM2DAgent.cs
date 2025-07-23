using System;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using Rewards;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Agents
{
    public class Sam2DAgent : Agent
    {
        public TwoDimensionalSamModel model;
        public GameObject target;
        private bool _atGoal;

        private IRewardFunction _denseReward;

        public override void OnEpisodeBegin()
        {
            _atGoal = false;

            bool overlapping = true;
            Vector3 startingPos = Vector3.zero;
            while (overlapping)
            {
                startingPos.x = Random.Range(-8.5f, 8.5f);
                startingPos.z = Random.Range(-8.5f, 8.5f);
                var overlapSphere = Physics.OverlapSphere(startingPos, 0.75f);
                overlapping = overlapSphere.Any(collision => collision.gameObject.CompareTag("wall") || collision.gameObject.CompareTag("Finish"));
            }

            model.Restart(startingPos, Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0)));
            model.SetInputs(0, 0);
            const float envDiameter = 29;
            _denseReward = new DifferenceReward(() => (target.transform.position - transform.position).magnitude, 1 / envDiameter);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            var observation = model.GetObservation();
            sensor.AddObservation(observation);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            model.SetInputs(actions.ContinuousActions[0] * 25, actions.ContinuousActions[1] * 7);


            var proximity = _denseReward.Compute();
            AddReward(0.5f * proximity);
            AddReward(-0.25f / MaxStep); // Time penalty

            if (model.HasCollided())
            {
                SetReward(-0.25f);
                EndEpisode();
            }

            if (_atGoal)
            {
                SetReward(0.5f);
                EndEpisode();
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var fromKeyboard2D = Inputs.FromKeyboard2D();
            var i = 0;

            var actionsOutContinuousActions = actionsOut.ContinuousActions;
            actionsOutContinuousActions[i] = fromKeyboard2D[i];
            actionsOutContinuousActions[++i] = fromKeyboard2D[i];
        }

        public void OnTriggerStay(Collider other)
        {
            _atGoal = other.gameObject.CompareTag("Finish");
        }
    }
}