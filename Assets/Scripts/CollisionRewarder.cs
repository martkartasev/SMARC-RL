using UnityEngine;

namespace DefaultNamespace
{
    public class CollisionRewarder : MonoBehaviour
    {
        public float collisionReward = 0.0f;

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.CompareTag("robot"))
            {
                collisionReward = -1;
            }
        }

        private void OnCollisionExit(Collision other)
        {
            if (other.gameObject.CompareTag("robot"))
            {
                collisionReward = 0f;
            }
        }
    }
}