using System;
using UnityEngine;

public class TwoDimensionalSamModel : MonoBehaviour
{
    public GameObject thruster;
    public GameObject propeller1;
    public GameObject propeller2;

    private float rps_current;
    private float theta_current;
    private Rigidbody rigidBody;

    public float l = 1.5f;
    public float diameter = 0.2f;
    public float Du = 3;
    public float Dv = 50;
    public float Dr = 10;

    public float Dprop = 0.14f;
    public float waterDensity = 1026f;
    public float KT0 = 0.4566f;
    public float forceCoefficient;
    public float forceBackwardsMultiplier = 0.1f;

    private float x, y, phi, u, v, r = 0.0f;

    void Awake()
    {
        if (forceCoefficient == 0) forceCoefficient = waterDensity * Mathf.Pow(Dprop, 4f) * KT0;
        rigidBody = GetComponent<Rigidbody>();

        x = transform.position.z;
        y = -transform.position.x;
        phi = -transform.localEulerAngles.y * Mathf.Deg2Rad;
        // rigidBody.inertiaTensor = new Vector3(0, 1f / 5f * rigidBody.mass * (Mathf.Pow(l / 2f, 2f) + Mathf.Pow(diameter / 2f, 2f)), 0);
    }

    void Start()
    {
    }

    public void SetInputs(float rps, float thruster_angle)
    {
        rps_current = Mathf.Clamp(rps, -25f, 25f);
        theta_current = Mathf.Clamp(thruster_angle, -7f, 7f);
    }

    void FixedUpdate()
    {
        ComputeModel(rps_current, theta_current);
        Visualize();
    }


    private void ComputeModel(float rps, float theta)
    {
        var localLinearVelocity = transform.InverseTransformVector(rigidBody.linearVelocity);
        var localAngularVelocity = rigidBody.angularVelocity;

        theta *= Mathf.Deg2Rad;
        float F = ComputeForce(rps);
        // float u = localLinearVelocity.z;
        // float v = -localLinearVelocity.x;
        // float r = -localAngularVelocity.y;
        float m = rigidBody.mass;
        float Iz = 1.7175f;

        var dt = Time.fixedDeltaTime;

        float x_dot = (u * Mathf.Cos(phi) + v * Mathf.Sin(phi)) * dt;
        float y_dot = (u * Mathf.Sin(phi) - v * Mathf.Cos(phi)) * dt;
        float phi_dot = r * dt;
        float u_dot = (0 * (-r * v) - (Du * u / m) + (1f / m * F * Mathf.Cos(theta)) * dt);
        float v_dot = (0 * (r * u) - (Dv * v / m) + (1f / m * F * Mathf.Sin(theta)) * dt);
        float r_dot = ((-r / Iz * Dr) - (1f / Iz * l / 2f * F * Mathf.Sin(theta))) * dt;
        
       
       

        Debug.Log(new Vector3(-y, 0, x) - transform.position + "    " + (-transform.localEulerAngles.y - phi * Mathf.Rad2Deg));

        x += x_dot;
        y += y_dot;
        phi += phi_dot;
        u += u_dot;
        v += v_dot;
        r += r_dot;

        rigidBody.position = new Vector3(-y, 0, x);
        rigidBody.rotation = Quaternion.Euler(0, -phi * Mathf.Rad2Deg, 0);
        // rigidBody.linearVelocity = transform.TransformVector(new Vector3(-(v + v_dot), 0, u + u_dot));
        // rigidBody.angularVelocity = new Vector3(0, -(r + r_dot), 0);
    }

    public float ComputeForce(float rps)
    {
        return rps > 0 ? forceCoefficient * Mathf.Pow(rps, 2f) : -forceCoefficient * Mathf.Pow(rps, 2f) * forceBackwardsMultiplier;
    }

    public void Visualize()
    {
        propeller1.transform.localRotation = Quaternion.Euler(propeller1.transform.localRotation.eulerAngles - new Vector3(0, 0, rps_current));
        propeller2.transform.localRotation = Quaternion.Euler(propeller2.transform.localRotation.eulerAngles + new Vector3(0, 0, rps_current));
        thruster.transform.localRotation = Quaternion.Euler(0f, -theta_current, 0f);
    }
}