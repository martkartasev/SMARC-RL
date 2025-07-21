using UnityEngine;

public static class Inputs
{
    public static float[] FromKeyboard2D()
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