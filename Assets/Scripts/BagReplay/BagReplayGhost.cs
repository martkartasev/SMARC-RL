using System;
using DefaultNamespace;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

namespace Inputs
{
    public enum ReplayType
    {
        Position,
        Velocity
    }

    public class BagReplayGhost : MonoBehaviour
    {
        public ArticulationChainComponent body;
        public BagReplay replay;
        public ReplayType type;

        private void Start()
        {
            body.Restart(ENU.ConvertToRUF(replay.positionROS), ENU.ConvertToRUF(replay.orientationROS));
        }

        private void FixedUpdate()
        {
            switch (type)
            {
                case ReplayType.Position:
                    DoPositionUpdate();
                    break;
                case ReplayType.Velocity:
                    DoVelocityUpdate();
                    break;
            }
        }

        private void DoVelocityUpdate()
        {
            body.GetRoot().linearVelocity = ENU.ConvertToRUF(replay.linearVelocityROS);
            body.GetRoot().angularVelocity = ENU.ConvertToRUF(replay.angularVelocityROS);
        }

        private void DoPositionUpdate()
        {
            if (replay.positionROS != Vector3.zero)
            {
                body.GetRoot().TeleportRoot(ENU.ConvertToRUF(replay.positionROS), ENU.ConvertToRUF(replay.orientationROS));
            }
        }
    }
}