using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class TwoDimensionalInputNorm : MonoBehaviour
    {
        public TwoDimensionalSamModel model;

        public void FixedUpdate()
        {
            float[] actions = FromKeyboard();
            
            model.SetInputs(actions[0] * 25, actions[1] * 7);
        }

        private static float[] FromKeyboard()
        {
            var actions = new float[2];
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

            if (Input.GetKey("left"))
            {
                actions[1] = -1f;
            }
            else if (Input.GetKey("right"))
            {
                actions[1] = 1f;
            }
            else
            {
                actions[1] = 0.0f;
            }

            return actions;
        }
    }
}