using System;
using Rewards;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Agents
{
    public class Sam2DAgent : Agent
    {
        public TwoDimensionalSamModel model;
        public GameObject target;
        public GameObject startPos;
        private bool _atGoal;
        private IRewardFunction _denseReward;

        public override void OnEpisodeBegin()
        {
            _atGoal = false;
            model.Restart(startPos.transform.position, startPos.transform.rotation);
            model.SetInputs(0, 0);
            const float envDiameter = 29;
            _denseReward = new DifferenceReward(() => (target.transform.position - transform.position).magnitude, 1 / envDiameter);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(model.GetObservation());
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            model.SetInputs(actions.ContinuousActions[0] * 25, actions.ContinuousActions[1] * 7);


            var proximity = _denseReward.Compute();
            AddReward(0.5f * proximity);

            if (model.HasCollided())
            {
                SetReward(-0.5f);
                EndEpisode();
            }

            if (_atGoal)
            {
                SetReward(0.5f);
                EndEpisode();
            }

           // Debug.Log(GetCumulativeReward());
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