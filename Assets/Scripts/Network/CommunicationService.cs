using System.Collections.Generic;
using System.Linq;
using ExternalCommunication;
using Google.Protobuf.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Action = ExternalCommunication.Action;
using Random = UnityEngine.Random;


namespace Network
{
    public class CommunicationService : MonoBehaviour
    {
        public static int decisionPeriod = 10;
        public static bool noGraphics = false;

        private Dictionary<int, EnvManager> _envManagers;
        private bool _processingStep;
        private int _stepsCompleted = 0;
        private int _stepsToSimulate = 0;
        private float _startTime;
        private float _timeScale;

        private EnvironmentSpawner _spawner;
        
        private bool _initialized = false;
        private bool _reloading = false;

        public void Initialize(RepeatedField<ResetParameters> resetMsgEnvsToReset)
        {
            _spawner = FindAnyObjectByType<EnvironmentSpawner>();
            _spawner.DoAwake(resetMsgEnvsToReset);

            _envManagers = _spawner.GetEnvs();
            _initialized = true;
        }

        public Observations DoReset(Reset resetMsg)
        {
            if (resetMsg.ReloadScene)
            {
                if (!_reloading)
                {
                    if (resetMsg.ReloadScene)
                    {
                        Random.InitState(0);
                        LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Single, LocalPhysicsMode.Physics3D);
                        SceneManager.LoadScene("ExternalControlScene", parameters);
                        _reloading = true;
                        _initialized = false;
                    }
                }

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    if (!SceneManager.GetSceneAt(i).isLoaded)
                    {
                        return null; // If any scene is loading, we must wait to avoid early / late calls causing "pure virtual call during construction".
                    }
                }

                _reloading = false;
            }


            Initialize(resetMsg.EnvsToReset);
            ResetEnvironments(resetMsg);
            SceneManager.GetActiveScene().GetPhysicsScene().Simulate(Time.fixedDeltaTime);

            _processingStep = false;
            _stepsCompleted = Mathf.Max(decisionPeriod, _stepsToSimulate);

            var prepareObservations = PrepareObservations();
            return prepareObservations;
        }

        public Observations DoStep(Step stepMsg)
        {
            if (!_processingStep)
            {
                ProcessActions(stepMsg.Actions);
                _processingStep = true;
                _stepsCompleted = 0;
                _stepsToSimulate = stepMsg.StepCount > 0 ? stepMsg.StepCount : decisionPeriod;
                _timeScale = stepMsg.TimeScale > 0 ? stepMsg.TimeScale : Time.timeScale;
                _startTime = Time.time;
            }


            var shouldSimStep = Time.time > (_startTime + _stepsCompleted * Time.fixedDeltaTime / _timeScale) || _timeScale > 10 || noGraphics;
            while (shouldSimStep && _stepsCompleted < _stepsToSimulate)
            {
                SceneManager.GetActiveScene().GetPhysicsScene().Simulate(Time.fixedDeltaTime);
                foreach (var env in _envManagers)
                {
                    env.Value.UpdateSync();
                }

                _stepsCompleted++;
            }

            if (_stepsCompleted >= _stepsToSimulate)
            {
                _processingStep = false;
                return PrepareObservations();
            }

            return null;
        }

        public Observations PrepareObservations()
        {
            var observations = new Observations();
            observations.Observations_.AddRange(_envManagers.Select(env => env.Value.BuildObservationMessage()));
            return observations;
        }

        public void ResetEnvironments(Reset reset)
        {
            if (reset.EnvsToReset == null || reset.EnvsToReset.Count == 0)
            {
                _envManagers.Values.ToList().ForEach(env => env.DoRestart());
            }
            else
            {
                for (int i = 0; i < reset.EnvsToReset.Count; i++)
                {
                    var resetParameters = reset.EnvsToReset[i];
                    var index = resetParameters.Index;
                    if (_envManagers.ContainsKey(index))
                    {
                        _envManagers[index].DoRestart(resetParameters);
                    }
                }
            }
        }

        private void ProcessActions(RepeatedField<Action> actions)
        {
            if (actions != null)
            {
                foreach (var action in actions.Select((msg, index) => new { msg, index }))
                {
                    _envManagers[action.index].RecieveAction(action.msg);
                }
            }
        }

        public bool IsInitialized()
        {
            return _initialized;
        }
    }
}