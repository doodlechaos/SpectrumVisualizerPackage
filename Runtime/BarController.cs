using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//[RequireComponent(typeof(Rigidbody), typeof(ConfigurableJoint))]
[RequireComponent(typeof(Rigidbody))]
public class BarController : MonoBehaviour //proportional–integral–derivative controller
{
    public Transform target;
    public Transform origin;
    public Transform stalk; 


    private Vector3 prevPos;
    public Transform spectrumVisualizerRoot;
    private SpectrumVisualizer sv;

    public void Start()
    {
        sv = spectrumVisualizerRoot.GetComponent<SpectrumVisualizer>();
    }
    public void InitBar(Transform _target, Transform _origin, Transform _stalk, Transform _spectrumVisualizerRoot)
    {
        target = _target;
        origin = _origin;
        stalk = _stalk;
        spectrumVisualizerRoot = _spectrumVisualizerRoot;

        GetComponent<Rigidbody>().isKinematic = true; 
    }

    public void SetInitialPosition()
    {
        stalk.position = origin.position;
        stalk.rotation = origin.rotation;
        GetComponent<Rigidbody>().MovePosition(origin.position);
        GetComponent<Rigidbody>().MoveRotation(origin.rotation);
    }

    public void MoveTowardsTarget()
    {
        //Debug.Log("moving towards target");
        GetComponent<Rigidbody>().MovePosition(Vector3.Lerp(transform.position, target.position, (sv == null) ? 1f : sv.rigidbodyLerpFraction));
    }


    public void UpdateBarStalk(float heightGlowStrength)
    {
        Vector3 rbPos = GetComponent<Rigidbody>().position;
        var dropDown = (origin.up * transform.localScale.y / 2);
        stalk.transform.rotation = origin.rotation; //This has nothing to do with the physics going through each other
        //if moving below origin or in edit mode, just set the stalk position and return
        if (Vector3.Dot((origin.position - rbPos), origin.up) > 0 || !Application.isPlaying)
        {
            stalk.transform.position = origin.position - dropDown;
            stalk.localScale = new Vector3(stalk.localScale.x, transform.localScale.y, stalk.localScale.z);
            return; 
        }
        //Debug.Log("updating bar stalk");
        //Else if above, change scale and position accordingly
        float length = Vector3.Distance(origin.position, rbPos);
        stalk.localScale = new Vector3(stalk.localScale.x, length + transform.localScale.y, stalk.localScale.z);
        stalk.transform.position = Vector3.Lerp(origin.position, rbPos, 0.5f) - dropDown; //Should be half way between the origin and cap

        //Update the color emission based on how long the length is
        Material currMat = GetComponent<Renderer>().sharedMaterial;
        //currMat.SetVector("_EmissiveColor", currMat.color * length / 5);
        currMat.SetVector("_EmissiveColor", currMat.color * heightGlowStrength);

        Material currMatStalk = stalk.GetComponent<Renderer>().sharedMaterial;
        //currMatStalk.SetVector("_EmissiveColor", currMat.color * length * heightGlowStrength);
        currMatStalk.SetVector("_EmissiveColor", currMat.color * heightGlowStrength);
    }

}
