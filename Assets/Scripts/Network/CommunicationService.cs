using System.Collections.Generic;
using System.Linq;
using ExternalCommunication;
using Google.Protobuf.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;


namespace Network
{
    public class CommunicationService : MonoBehaviour
    {
        public static int decisionPeriod = 10;
        public static bool noGraphics = false;

        private Dictionary<int, AbstractEnvManager> _envManagers;

        private bool _initialized;
        private bool _processingStep;
        private bool _reloading;

        private EnvironmentSpawner _spawner;
        private float _startTime;
        private int _stepsCompleted;
        private int _stepsToSimulate;
        private float _timeScale;

        public void Initialize(RepeatedField<ResetParameters> resetMsgEnvsToReset)
        {
            if (!_initialized)
            {
                _spawner = FindAnyObjectByType<EnvironmentSpawner>();
                _spawner.DoAwake(resetMsgEnvsToReset);
                _envManagers = _spawner.GetEnvs();
            }

            _initialized = true;
        }

        public Observations DoReset(Reset resetMsg)
        {
            if (resetMsg.ReloadScene || !_initialized)
            {
                if (!_reloading)
                    if (resetMsg.ReloadScene)
                    {
                        Random.InitState(0);
                        var parameters = new LoadSceneParameters(LoadSceneMode.Single, LocalPhysicsMode.Physics3D);
                        SceneManager.LoadScene("ExternalControlScene", parameters);
                        _reloading = true;
                        _initialized = false;
                    }

                for (var i = 0; i < SceneManager.sceneCount; i++)
                    if (!SceneManager.GetSceneAt(i).isLoaded)
                        return null; // If any scene is loading, we must wait to avoid early / late calls causing "pure virtual call during construction".

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
            if (!_initialized) return null;

            if (!_processingStep)
            {
                ProcessActions(stepMsg.Actions);
                _processingStep = true;
                _stepsCompleted = 0;
                _stepsToSimulate = stepMsg.StepCount > 0 ? stepMsg.StepCount : decisionPeriod;
                _timeScale = stepMsg.TimeScale > 0 ? stepMsg.TimeScale : Time.timeScale;
                _startTime = Time.time;
            }
            
            while (Time.time > _startTime + _stepsCompleted * Time.fixedDeltaTime / _timeScale || _timeScale > 10 || noGraphics && _stepsCompleted < _stepsToSimulate)
            {
                Debug.Log("Calling Simulate Step");
                SceneManager.GetActiveScene().GetPhysicsScene().Simulate(Time.fixedDeltaTime);
                foreach (var env in _envManagers) env.Value.FixedUpdateManual();

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
            observations.Observations_.AddRange(_envManagers.Select(env => env.Value).Select(env => env.GetMessageMapper().MapObservationToExternal(env.BuildObservationMessage())));
            return observations;
        }

        public void ResetEnvironments(Reset reset)
        {
            if (reset.EnvsToReset == null || reset.EnvsToReset.Count == 0)
                _envManagers.Values.ToList().ForEach(env => env.DoRestart(env.GetMessageMapper().MapReset(new ResetParameters())));
            else
                for (var i = 0; i < reset.EnvsToReset.Count; i++)
                {
                    var resetParameters = reset.EnvsToReset[i];
                    var index = resetParameters.Index;
                    if (_envManagers.ContainsKey(index))
                    {
                        var env = _envManagers[index];
                        env.DoRestart(env.GetMessageMapper().MapReset(resetParameters));
                    }
                }
        }

        private void ProcessActions(RepeatedField<ExternalCommunication.Action> actions)
        {
            if (actions != null)
                foreach (var action in actions.Select((msg, index) => new { msg, index }))
                {
                    var env = _envManagers[action.index];
                    env.ReceiveAction(env.GetMessageMapper().MapAction(action.msg));
                }
        }

        public bool IsInitialized()
        {
            return _initialized;
        }
    }
}