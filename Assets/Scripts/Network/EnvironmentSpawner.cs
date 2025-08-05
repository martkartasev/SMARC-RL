using System.Collections.Generic;
using System.Linq;
using ExternalCommunication;
using Google.Protobuf.Collections;
using Unity.VisualScripting;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Transform = UnityEngine.Transform;
using Vector3 = UnityEngine.Vector3;

namespace Network
{
    public class EnvironmentSpawner : MonoBehaviour
    {
        public EnvManager environmentPrefab;

        public int gridLength = 5;
        public int stepSize = 3;

        private Dictionary<int, EnvManager> envs = new();

        public static int defaultAgents = 1;


        public void DoAwake(RepeatedField<ResetParameters> resetMsgEnvsToReset)
        {
            ClearAgents();

            if (resetMsgEnvsToReset != null && resetMsgEnvsToReset.Count > 0)
            {
                SpawnAgents(resetMsgEnvsToReset);
            }
            else
            {
                SpawnAgents(Enumerable.Range(0, defaultAgents).ToList());
            }
        }

        private void SpawnAgents(List<int> agentIds)
        {
            foreach (var i in agentIds)
            {
                var instantiate = Instantiate(environmentPrefab, new Vector3(stepSize * (i % gridLength), 0, gridLength * (i / gridLength)) * 2, Quaternion.identity);
                envs.Add(i, instantiate.GetComponent<EnvManager>());
            }
        }


        private void SpawnAgents(RepeatedField<ResetParameters> envParameters)
        {
            List<int> agentIds = new();
            foreach (var resetParameters in envParameters)
            {
                int i = resetParameters.Index;
                var instantiate = Instantiate(DeterminePrefab(envParameters[i]), new Vector3(stepSize * (i % gridLength), 0, gridLength * (i / gridLength)) * 2, Quaternion.identity);
                envs.Add(i, instantiate.GetComponent<EnvManager>());

                agentIds.Add(i);
            }

            var remainingIds = Enumerable.Range(0, defaultAgents).Except(agentIds);
            SpawnAgents(remainingIds.ToList());
        }

        private Object DeterminePrefab(ResetParameters envParameter)
        {
            return environmentPrefab; //TODO Temp
        }

        public void ClearAgents()
        {
            envs.Values.ToList().ForEach(manager => Destroy(manager.gameObject));
            envs = new Dictionary<int, EnvManager>();
        }

        private Bounds EncapsulateGameObjects(List<GameObject> gameObjects)
        {
            var mapBoundsHelper = gameObjects[0].GetComponent<Renderer>().bounds;
            foreach (GameObject renderer in gameObjects)
            {
                var rendererBounds = renderer.transform.GetComponent<Renderer>();
                if (rendererBounds != null)
                {
                    mapBoundsHelper.Encapsulate(rendererBounds.bounds);
                }
                else
                {
                    foreach (var componentsInChild in renderer.GetComponentsInChildren<Renderer>())
                    {
                        mapBoundsHelper.Encapsulate(componentsInChild.bounds);
                    }
                }
            }

            return mapBoundsHelper;
        }

        public static List<GameObject> FindAllChildrenWithComponent<T>(Transform item)
        {
            var objects = new List<GameObject>();
            foreach (Transform child in item)
            {
                if (item.GetComponent<T>() != null)
                {
                    objects.Add(child.gameObject);
                }

                objects.AddRange(FindAllChildrenWithComponent<T>(child));
            }

            return objects;
        }

        public Dictionary<int, EnvManager> GetEnvs()
        {
            return envs;
        }
    }
}