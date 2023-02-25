using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(ConfigurableJoint))]
public class BarController : MonoBehaviour //proportional–integral–derivative controller
{
    public Transform target;
    public Transform origin; 

    public float pGain;
    public float dGain;

    private float lastError = 0f;


    public void InitBar(Transform _target, Transform _origin, float _pGain, float _dGain)
    {
        target = _target;
        origin = _origin;
        pGain = _pGain;
        dGain = _dGain;

        GetComponent<ConfigurableJoint>().anchor = Vector3.zero;
        GetComponent<ConfigurableJoint>().autoConfigureConnectedAnchor = false;
        GetComponent<ConfigurableJoint>().xMotion = ConfigurableJointMotion.Limited;
        GetComponent<ConfigurableJoint>().yMotion = ConfigurableJointMotion.Locked;
        GetComponent<ConfigurableJoint>().zMotion = ConfigurableJointMotion.Locked;
        GetComponent<ConfigurableJoint>().angularXMotion = ConfigurableJointMotion.Locked;
        GetComponent<ConfigurableJoint>().angularYMotion = ConfigurableJointMotion.Locked;
        GetComponent<ConfigurableJoint>().angularZMotion = ConfigurableJointMotion.Locked;
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
        GetComponent<Rigidbody>().AddForce(force * (target.position - transform.position).normalized, ForceMode.Force); //PID Controller

        // Remember the last error for the next FixedUpdate
        lastError = error;
    }

    public void UpdateConfigJoint(float sliderHeightLimit)
    {
        GetComponent<ConfigurableJoint>().connectedAnchor = origin.transform.position;

        GetComponent<ConfigurableJoint>().axis = Vector3.up; // (currBarOrigin.position - currBarTarget.position); //normal;
        SoftJointLimit sjl = new SoftJointLimit();
        sjl.limit = sliderHeightLimit;
        GetComponent<ConfigurableJoint>().linearLimit = sjl;


        GetComponent<ConfigurableJoint>().angularXMotion = ConfigurableJointMotion.Free;
        GetComponent<ConfigurableJoint>().angularYMotion = ConfigurableJointMotion.Free;
        GetComponent<ConfigurableJoint>().angularZMotion = ConfigurableJointMotion.Free;
        transform.rotation = origin.rotation;
        GetComponent<ConfigurableJoint>().angularXMotion = ConfigurableJointMotion.Locked;
        GetComponent<ConfigurableJoint>().angularYMotion = ConfigurableJointMotion.Locked;
        GetComponent<ConfigurableJoint>().angularZMotion = ConfigurableJointMotion.Locked;

    }

}
