using System;
using Force;
using Reward;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    [RequireComponent(typeof(DecisionRequester))]
    public class SAMGeneralMovementLearningAgent : Agent
    {
        [HideInInspector] public float maxDistance = 5f;
        [HideInInspector] public Vector3 initMax = new(5, 5, 5);
        [HideInInspector] public Vector3 initMin = new(-5, -5, -5);

        [Header("Target Speed")] [Range(0.1f, 0.5f)] [SerializeField]
        //The walking speed to try and achieve
        public float targetSpeed = 1;

        public bool randomizeSpeed = true;
        public Transform targetObject;
        public VehicleComponents.ROS.Publishers.Odometry_Pub odometry;

        public ArticulationBody body;
        private SAMUnityNormalizedController samControl;

        protected IRewardFunction _improvement;
        private int decisionPeriod;
        private ArticulationChainComponent articulationChain;
        private bool resetBody;
        private bool beenAtGoal;

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
            beenAtGoal = false;
            _improvement = new ImprovingReward(() => (targetObject.position - body.transform.position).magnitude); // Give reward for distance, when closer than "maximum distance" for reward. Scales linearly.
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
            // ODOM. Usually where we do the first GPS ping after coming online.
            var pose = odometry.GetRosMsg().pose.pose;
            var twist = odometry.GetRosMsg().twist.twist;

            sensor.AddObservation(
                new Quaternion((float) pose.orientation.x,
                    (float) pose.orientation.y,
                    (float) pose.orientation.z,
                    (float) pose.orientation.w));

            sensor.AddObservation(new Vector3(
                (float) twist.linear.x,
                (float) twist.linear.y,
                (float) twist.linear.z).ForceNormalizeVector());

            sensor.AddObservation((new Vector3(
                (float) twist.angular.x,
                (float) twist.angular.y,
                (float) twist.angular.z) / 0.5f).ForceNormalizeVector());
            // Normalize distance vector such that everything more than maxDistance meters away is "the same"
            sensor.AddObservation((body.transform.InverseTransformVector(targetObject.position - body.transform.position) / maxDistance).To<FLU>().ToUnityVec3().ForceNormalizeVector());

            sensor.AddObservation(targetSpeed / 0.5f);
        }


        public override void OnActionReceived(ActionBuffers actions)
        {
            SetControlInputs(actions);

            var reward = ComputeReward();

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

        protected virtual float ComputeReward()
        {
            // We manually ensure the rewards never exceed the -1 : 1 range during an episode.
            // This is for stability in the neural networks.
            // We dont use normalization in the network inputs, and do it manually ourselves.
            // Doing it ourselves, we dont have to "learn" what the possible range of values is.
            // IF you do this manually, make sure to turn off normalization in the learning config file.


            var reward = 0.0f;

            if (beenAtGoal || (targetObject.position - body.transform.position).magnitude < 2f)
            {
                beenAtGoal = true;
                reward += Mathf.Clamp(1 - (targetObject.position - body.transform.position).magnitude / 2f, -1, 1) / MaxStep;
                // Currently unused "align with target" reward. Currently insufficient observation for this, cant enable
                // reward += 0.xf * ((Vector3.Dot(targetObject.forward, body.transform.forward) + 1) * 0.5f); 
            }

            if (!beenAtGoal)
            {
                reward += 0.5f * _improvement.Compute();
                reward += 0.5f * VelocityReward() / MaxStep;
                reward += -0.5f / MaxStep; // Time penalty.
            }


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
            var i = 0;
            var actionsContinuousAction = actions.ContinuousActions[i++];
            // Debug.Log( actions.ContinuousActions[0] + " : " + actions.ContinuousActions[1] + " : " + actions.ContinuousActions[2] + " : " + actions.ContinuousActions[3]);
            samControl.SetRpm(actionsContinuousAction, actionsContinuousAction);
            samControl.SetWaterPump((actions.ContinuousActions[i++] + 1) / 2);
            samControl.SetElevatorAngle(actions.ContinuousActions[i++]);
            samControl.SetRudderAngle(actions.ContinuousActions[i++]);
            samControl.SetBatteryPack((actions.ContinuousActions[i++] + 1) / 2);
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