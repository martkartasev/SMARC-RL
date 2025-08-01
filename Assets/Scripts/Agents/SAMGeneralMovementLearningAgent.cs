using System;
using DefaultNamespace;
using Force;
using Rewards;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Agents
{
    [RequireComponent(typeof(DecisionRequester))]
    public class SAMGeneralMovementLearningAgent : Agent
    {
        [HideInInspector] public float maxDistance = 45f;
        [HideInInspector] public Vector3 initMax = new(5, 5, 5);
        [HideInInspector] public Vector3 initMin = new(-5, -5, -5);

        [Header("Target Speed")] [Range(0.1f, 0.5f)] [SerializeField]
        //The walking speed to try and achieve
        public float targetSpeed = 1;

        public bool randomizeSpeed = true;
        public Transform targetObject;

        public ArticulationBody body;
        private SAMUnityNormalizedController samControl;

        protected IRewardFunction _distancePenalty;
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
            targetSpeed = Math.Max(0.1f, Math.Min(0.5f, targetSpeed));
        }

        public override void OnEpisodeBegin()
        {
            Vector3 newPos = new Vector3(Random.Range(initMin.x, initMax.x), Random.Range(initMin.y, initMax.y), Random.Range(initMin.z, initMax.z));
            samControl.SetRpm(0, 0);
            samControl.SetBatteryPack(0.5f);
            samControl.SetWaterPump(0.5f);
            samControl.SetElevatorAngle(0);
            samControl.SetRudderAngle(0);

            articulationChain.root.immovable = true;
            articulationChain.Restart(transform.position + newPos, Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0)));
            resetBody = true;
            InitializeTarget();
            _distancePenalty = new DistancePenalty(() => (targetObject.position - body.transform.position).magnitude, maxDistance); // Give reward for distance, when closer than "maximum distance" for reward. Scales linearly.
        }

        protected virtual void InitializeTarget()
        {
            if (randomizeSpeed) targetSpeed = Random.Range(0.1f, 0.5f);
            targetObject.localPosition = new Vector3(Random.Range(-15, 15), Random.Range(-15, 15), Random.Range(-15, 15));
            targetObject.localRotation = Quaternion.Euler(new Vector3(Random.Range(-15, 15), Random.Range(0, 360), 0));
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
            sensor.AddObservation(body.transform.rotation.To<ENU>().ToUnityQuaternion());

            var linearVelocityBody = body.transform.InverseTransformVector(body.linearVelocity * 1.5f).To<FLU>().ToUnityVec3().ForceNormalizeVector();
            sensor.AddObservation(linearVelocityBody);
            var angularVelocityBody = body.transform.InverseTransformVector(body.angularVelocity * 3f).To<FLU>().ToUnityVec3().ForceNormalizeVector();
            sensor.AddObservation(angularVelocityBody);

            var targetPositionBody = (body.transform.InverseTransformVector(targetObject.position - body.transform.position) / maxDistance).To<ENU>().ToUnityVec3().ForceNormalizeVector();
            sensor.AddObservation(targetPositionBody);
            var targetOrientationBody = (Quaternion.Inverse(body.transform.rotation) * targetObject.rotation).To<ENU>().ToUnityQuaternion();
            sensor.AddObservation(targetOrientationBody);

            sensor.AddObservation(targetSpeed / 0.5f);
        }


        public override void OnActionReceived(ActionBuffers actions)
        {
            SetControlInputs(actions);

            var reward = ComputeReward(actions);

            if (float.IsNaN(reward))
            {
                Debug.Log("Warning! Reward NaN! Something went wrong!");
            }
            else
            {
                // Debug.Log(GetCumulativeReward()); //NB! NEVER LEAVE CONSTANT DEBUG MESSAGES ENABLED, It causes massive slowdowns
                AddReward(reward);
            }

            if ((Vector3.zero - transform.InverseTransformPoint(body.transform.position)).magnitude > 45)
            {
                SetReward(-0.25f);
                EndEpisode();
            }
        }

        protected virtual float ComputeReward(ActionBuffers actions)
        {
            // We manually ensure the rewards never exceed the -1 : 1 range during an episode.
            // This is for stability in the neural networks.
            // We dont use normalization in the network inputs, and do it manually ourselves.
            // Doing it ourselves, we dont have to "learn" what the possible range of values is.
            // IF you do this manually, make sure to turn off normalization in the learning config file.

            var normalized01AlignmentReward = (Vector3.Dot(targetObject.forward, body.transform.forward) + 1) * 0.5f;
            var reward = _distancePenalty.Compute() * normalized01AlignmentReward / MaxStep;
            // reward += VelocityReward() / MaxStep * 0.2f;

            // reward += -0.5f / MaxStep; // Time penalty.


            return reward;
        }

        protected virtual float VelocityReward()
        {
            var matchSpeed = GetMatchingVelocityReward(body.transform.forward * targetSpeed, body.linearVelocity);
            // Speed for having a velocity magnitude matching the configured one

            var lookAtTarget = (Vector3.Dot((targetObject.position - body.transform.position).normalized, body.transform.forward) + 1) * 0.5f;
            // Dot the vectors for matching the forward vector of SAM with a vector towards the goal. This is not strictly necessary, but we "prefer" this behaviour. SAM is technically easier to maneuver backwards in many cases. 

            var reward = matchSpeed * lookAtTarget;
            // Halve this reward, so the alignment reward for being close becomes "greater" i.e a better state to be in
            return reward;
        }


        protected float GetMatchingVelocityReward(Vector3 velocityGoal, Vector3 actualVelocity)
        {
            var velDeltaMagnitude = Mathf.Clamp(Vector3.Distance(actualVelocity, velocityGoal), 0, targetSpeed);

            //Return a value on a declining curve that decays from 1 to 0
            //This reward will approach 1 if it matches perfectly and approach zero as it deviates
            return Mathf.Pow(1 - Mathf.Pow(velDeltaMagnitude / targetSpeed, 2), 2);
        }

        private void SetControlInputs(ActionBuffers actions)
        {
            var actionIndex = 0;

            var actionsContinuousAction = actions.ContinuousActions[actionIndex++];
            samControl.SetRpm(actionsContinuousAction, actionsContinuousAction);
            samControl.SetWaterPump((actions.ContinuousActions[actionIndex++] + 1) / 2);
            samControl.SetElevatorAngle(actions.ContinuousActions[actionIndex++]);
            samControl.SetRudderAngle(actions.ContinuousActions[actionIndex++]);
            samControl.SetBatteryPack((actions.ContinuousActions[actionIndex++] + 1) / 2);
        }

        //Simple inputs for local testing. Could be replaced with the new InputSystem.
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