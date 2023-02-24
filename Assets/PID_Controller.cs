using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PID_Controller : MonoBehaviour //proportional–integral–derivative controller
{
    public Transform target;

    public float pGain;
    public float dGain;

    private float lastError = 0f;

    public void SetTarget(Transform _target)
    {
        target = _target;
    }

    public void SetGains(float _pGain, float _dGain)
    {
        pGain = _pGain;
        dGain = _dGain;
    }

    private void FixedUpdate()
    {
        if (target == null)
            return;

        // Calculate the error between the target position and the current position
        float error = Vector3.Distance(target.position, transform.position);

        // Calculate the derivative of the error
        float errorDerivative = (error - lastError) / Time.fixedDeltaTime;

        // Calculate the force needed to move towards the target position
        float force = error * pGain + errorDerivative * dGain;

        // Apply the force to the Rigidbody
        GetComponent<Rigidbody>().AddForce(force * (target.position - transform.position).normalized, ForceMode.Force);

        // Remember the last error for the next FixedUpdate
        lastError = error;
    }

}
