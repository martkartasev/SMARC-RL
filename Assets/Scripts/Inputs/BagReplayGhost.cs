using System;
using DefaultNamespace;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

namespace Inputs
{
    public class BagReplayGhost : MonoBehaviour
    {
        public ArticulationChainComponent body;
        public BagReplay replay;

        private void Start()
        {
            body.Restart(ENU.ConvertToRUF(replay.positionROS), ENU.ConvertToRUF(replay.orientationROS));
        }

        private void FixedUpdate()
        {
            if (replay.positionROS != Vector3.zero)
            {
                body.GetRoot().TeleportRoot(ENU.ConvertToRUF(replay.positionROS), ENU.ConvertToRUF(replay.orientationROS));
            }
        }
    }
}