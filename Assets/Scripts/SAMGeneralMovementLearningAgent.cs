using Force;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    [RequireComponent(typeof(DecisionRequester))]
    public class SAMGeneralMovementLearningAgent : Agent
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
            Vector3 newPos = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5));
            samControl.SetRpm(0, 0);
            samControl.SetBatteryPack(0.5f);
            samControl.SetWaterPump(0.5f);
            samControl.SetElevatorAngle(0);
            samControl.SetRudderAngle(0);

            articulationChain.root.immovable = true;
            articulationChain.Restart(transform.position + newPos, Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0)));
            resetBody = true;
          
            InitializeTarget();
        }

        private void InitializeTarget()
        {
            targetSpeed = Random.Range(0.1f, 0.5f);
            targetObject.localPosition = new Vector3(Random.Range(-15, 15), Random.Range(-15, 15), Random.Range(-15, 15));
            targetObject.localRotation = Quaternion.Euler(new Vector3(Random.Range(-15, 15), Random.Range(0, 360), 0));
            initialDistance = (body.transform.position - targetObject.position).magnitude;
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
            //TODO: Replace with sensor data from Vehicle
            sensor.AddObservation(body.transform.localPosition / 45);
            sensor.AddObservation(body.transform.localRotation.eulerAngles / 360);
            sensor.AddObservation(targetObject.localRotation);
            sensor.AddObservation(body.transform.InverseTransformVector(body.velocity) / 0.5f);
            sensor.AddObservation(body.transform.InverseTransformVector(body.angularVelocity) / 0.3f);
            sensor.AddObservation((body.transform.position - targetObject.position) / 90);
            sensor.AddObservation(targetSpeed / 0.5f);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            SetControlInputs(actions);
            
            var reward = ComputeReward();
            if (float.IsNaN(reward))
            {
                Debug.Log("Warning nan");
            }
            else
            {
                AddReward(reward / 2 / Mathf.Max(2500, MaxStep) * decisionPeriod);
            }
            
            if ((Vector3.zero - body.transform.localPosition).magnitude > 45)
            {
                EndEpisode();
            }
        }

        private float ComputeReward()
        {
            var reward = Mathf.Max(0, 1 - (targetObject.position - body.transform.position).magnitude / initialDistance);
            
            if ((targetObject.localPosition - body.transform.localPosition).magnitude < 1f)
            {
                reward += 1;
            }
            else
            {
                var matchSpeedReward = GetMatchingVelocityReward(body.transform.forward * targetSpeed, body.velocity);
                var lookAtTargetReward = (Vector3.Dot((targetObject.localPosition - body.transform.localPosition).normalized, body.transform.forward) + 1);
                reward += matchSpeedReward * lookAtTargetReward;
            }

            return reward;
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