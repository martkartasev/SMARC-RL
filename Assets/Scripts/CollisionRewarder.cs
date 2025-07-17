using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class CollisionRewarder : MonoBehaviour
    {
        [FormerlySerializedAs("collisionReward")] public float collisionPenalty = 0.0f;

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.CompareTag("robot"))
            {
                collisionPenalty = -1;
            }
        }

        private void OnCollisionExit(Collision other)
        {
            if (other.gameObject.CompareTag("robot"))
            {
                collisionPenalty = 0f;
            }
        }
    }
}