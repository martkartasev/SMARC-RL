using System;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using Rewards;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
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
        private float[] previous_action = new float[2];

        public override void OnEpisodeBegin()
        {
            _atGoal = false;

            bool overlapping = true;
            Vector3 startingPos = Vector3.zero;
            while (overlapping)
            {
                startingPos.x = Random.Range(-7f, 7f);
                startingPos.z = Random.Range(-7f, 7f);
                var overlapSphere = Physics.OverlapSphere(startingPos, 0.75f);
                overlapping = overlapSphere.Any(collision => collision.gameObject.CompareTag("wall") || collision.gameObject.CompareTag("Finish"));
            }

            model.Restart(startingPos, Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0)));
            model.SetInputs(0, 0);
            const float envDiameter = 19.8f;
            _denseReward = new DifferenceReward(() => (target.transform.position - transform.position).magnitude, 1/19.8f);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            var observation = model.GetObservation();
            sensor.AddObservation(observation);
            sensor.AddObservation(previous_action);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            model.SetInputs(actions.ContinuousActions[0] * 25, actions.ContinuousActions[1] * 7);

            ComputeReward(actions);

            previous_action = actions.ContinuousActions.Array.Clone() as float[];
        }

        private void ComputeReward(ActionBuffers actions)
        {
            var dense = _denseReward.Compute();
            
            var actuatorPenalty = 0f;
            for (int i = 0; i < previous_action.Length; i++)
            {
                actuatorPenalty += Mathf.Abs(previous_action[i] - actions.ContinuousActions[i]);
            }

            actuatorPenalty /= 2;


            var f = 0.5f * dense;
            AddReward(f);
            AddReward(-0.1f / MaxStep); // Time penalty
            AddReward(-0.5f * actuatorPenalty / MaxStep);

            Debug.Log(GetCumulativeReward());
            if (model.HasCollided())
            {
                SetReward(-0.4f);
                EndEpisode();
            }

            if (_atGoal)
            {
                SetReward(0.5f);
                Debug.Log(GetCumulativeReward());
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