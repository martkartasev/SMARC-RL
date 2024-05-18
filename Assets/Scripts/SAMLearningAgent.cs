using System;
using Force;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    [RequireComponent(typeof(DecisionRequester))]
    public class SAMLearningAgent : Agent
    {
        [Header("Target Speed")] [Range(0.1f, 0.5f)] [SerializeField]
        //The walking speed to try and achieve
        public float targetSpeed = 1;

        public Transform targetObject;

        public ArticulationBody body;
        private SAMUnityNormalizedController samControl;

        private float initialDistance;
        private int decisionPeriod;
        private ArticulationChainComponent articulationChain;
        private bool resetBody;

        protected override void Awake()
        {
            base.Awake();
            samControl = GetComponent<SAMUnityNormalizedController>();
            articulationChain = GetComponent<ArticulationChainComponent>();
            var decisionRequester = gameObject.GetComponent<DecisionRequester>();
            decisionPeriod = decisionRequester.DecisionPeriod;
            decisionRequester.DecisionStep = Random.Range(0, decisionPeriod - 1);
        }

        public override void OnEpisodeBegin()
        {
            transform.localPosition = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5));
            samControl.SetRpm(0, 0);
            samControl.SetBatteryPack(0.5f);
            samControl.SetWaterPump(0.5f);
            samControl.SetElevatorAngle(0);
            samControl.SetRudderAngle(0);
            
            articulationChain.Restart(transform.position, Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0)));
            resetBody = true;
          
            InitializeTarget();
        }

        private void InitializeTarget()
        {
            targetSpeed = Random.Range(0.1f, 0.5f);
            targetObject.localPosition = new Vector3(Random.Range(-15, 15), Random.Range(-15, 15), Random.Range(-15, 15));
            targetObject.localRotation = Quaternion.Euler(new Vector3(Random.Range(-15, 15), Random.Range(0, 360), 0));
            initialDistance = (transform.localPosition - targetObject.localPosition).magnitude;
        }

        private void FixedUpdate()
        {
            if (resetBody)
            {
                body.immovable = false;
                resetBody = false;
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(body.transform.localPosition / 45);
            sensor.AddObservation(body.transform.localRotation);
            sensor.AddObservation(targetObject.localRotation);
            sensor.AddObservation(body.transform.InverseTransformDirection(body.velocity) / 0.5f);
            sensor.AddObservation(body.transform.InverseTransformDirection(body.angularVelocity) / 0.3f);
            sensor.AddObservation((body.transform.localPosition - targetObject.localPosition) / 45);
            sensor.AddObservation(targetSpeed / 0.5f);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            SetControlInputs(actions);

            var reward = Mathf.Max(0, 1 - (targetObject.localPosition - transform.localPosition).magnitude / initialDistance);


            if ((targetObject.localPosition - transform.localPosition).magnitude < 0.5f)
            {
                reward += 1;
            }
            else
            {
                var matchSpeedReward = GetMatchingVelocityReward(transform.forward * targetSpeed, body.velocity);
                var lookAtTargetReward = (Vector3.Dot((targetObject.localPosition - transform.localPosition).normalized, transform.forward) + 1) * .5F;
                reward += matchSpeedReward * lookAtTargetReward;
            }

            if ((Vector3.zero - transform.localPosition).magnitude > 45)
            {
                reward += -50;
                EndEpisode();
            }

            if (float.IsNaN(reward))
            {
                Debug.Log("Warning nan");
            }
            else
            {
                AddReward(reward / Mathf.Max(2500, MaxStep) * decisionPeriod);
            }
        }


        public float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
        {
            var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, velocityGoal), 0, targetSpeed);

            //return the value on a declining sigmoid shaped curve that decays from 1 to 0
            //This reward will approach 1 if it matches perfectly and approach zero as it deviates
            return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / targetSpeed, 2), 2);
        }

        private void SetControlInputs(ActionBuffers actions)
        {
            var i = 0;
            var actionsContinuousAction = actions.ContinuousActions[i++];
            // Debug.Log( actions.ContinuousActions[0] + " : " + actions.ContinuousActions[1] + " : " + actions.ContinuousActions[2] + " : " + actions.ContinuousActions[3]);
            samControl.SetRpm(actionsContinuousAction, actionsContinuousAction);
            samControl.SetWaterPump((actions.ContinuousActions[i++] + 1) / 2);
            samControl.SetElevatorAngle(actions.ContinuousActions[i++]);
            samControl.SetRudderAngle(actions.ContinuousActions[i++]);
            samControl.SetBatteryPack((actions.ContinuousActions[i++] + 1) / 2);
        }


        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var actions = actionsOut.ContinuousActions;
            if (Input.GetKey("down"))
            {
                actions[0] = -0.8f;
            }
            else if (Input.GetKey("up"))
            {
                actions[0] = 0.8f;
            }
            else
            {
                actions[0] = 0.0f;
            }

            if (Input.GetKey("c"))
            {
                actions[1] = -1;
            }
            else if (Input.GetKey("space"))
            {
                actions[1] = 1;
            }
            else
            {
                actions[1] = 0;
            }

            if (Input.GetKey("w"))
            {
                actions[2] = -1;
            }
            else if (Input.GetKey("s"))
            {
                actions[2] = 1;
            }
            else
            {
                actions[2] = 0;
            }

            if (Input.GetKey("a"))
            {
                actions[3] = -1;
            }
            else if (Input.GetKey("d"))
            {
                actions[3] = 1;
            }
            else
            {
                actions[3] = 0;
            }
        }
    }
}