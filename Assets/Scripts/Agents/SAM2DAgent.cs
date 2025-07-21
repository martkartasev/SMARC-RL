using System;
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

        public override void OnEpisodeBegin()
        {
            _atGoal = false;
            model.Restart(startPos.transform.position, startPos.transform.rotation);
            model.SetInputs(0, 0);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(model.GetObservation());
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            model.SetInputs(actions.ContinuousActions[0] * 25, actions.ContinuousActions[1] * 7);
            if (model.HasCollided())
            {
                SetReward(-0.9f);
                EndEpisode();
                return;
            }

            if (_atGoal)
            {
                SetReward(0.25f);
                EndEpisode();
                return;
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