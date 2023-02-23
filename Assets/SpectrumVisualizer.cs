using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AudioSource))]
public class SpectrumVisualizer : MonoBehaviour
{

    [SerializeField] private bool physicsEnabled; 

    [HideInInspector] public AudioClip inputAudio;
    [Space(15), Range(64, 8192)]

    [SerializeField] public int visualizerSamples = 64;
    [SerializeField] public FFTWindow fttwindow;

    public enum AudioInputMode { AudioFile, LiveListen, Microphone } 
    [HideInInspector] public AudioInputMode audioInputMode;
    private AudioInputMode previousAudioInputMode;

    public enum UpdateMode { ManualUpdate, UpdateEachFrame }
    [SerializeField] public UpdateMode updateMode;

    private List<GameObject> deathRow = new List<GameObject>();

    [SerializeField] private int totalBars;
    [SerializeField] private float barWidth;
    private float prevBarWidth;

    [SerializeField] private float barHeightMultiplier;


    private LineRenderer lr;
    private Transform BarOriginsRoot;
    private Transform BarRigidbodiesRoot;
    private Transform PurgatoryRoot;

    [SerializeField] private bool testButton;

    public void CustomOnValidate()
    {
        if (testButton)
        {
            testButton = false;
            BarOriginsRoot.localScale = Vector3.one;
        }
        //Build the roots if they don't exist yet
        if (transform.childCount <= 0 || transform.GetChild(0).name != "BarOriginsRoot")
        {
            BarOriginsRoot = new GameObject("BarOriginsRoot").transform;
            BarOriginsRoot.position = Vector3.zero;
            BarOriginsRoot.localScale = Vector3.one;
            BarOriginsRoot.SetParent(transform);

        }
        else
        {
            BarOriginsRoot = transform.GetChild(0); 
        }
        //Build the roots if they don't exist yet
        if (transform.childCount <= 1 || transform.GetChild(1).name != "BarRigidbodiesRoot")
        {
            BarRigidbodiesRoot = new GameObject("BarRigidbodiesRoot").transform;
            BarRigidbodiesRoot.position = Vector3.zero;
            BarRigidbodiesRoot.localScale = Vector3.one;
            BarRigidbodiesRoot.SetParent(transform);
        }
        else
        {
            BarRigidbodiesRoot = transform.GetChild(1);
        }
        if (transform.childCount <= 2 || transform.GetChild(2).name != "PurgatoryRoot")
        {
            PurgatoryRoot = new GameObject("PurgatoryRoot").transform;
            PurgatoryRoot.SetParent(transform);
            PurgatoryRoot.transform.position = Vector3.zero;
            PurgatoryRoot.transform.localScale = Vector3.one;
        }
        else
        {
            PurgatoryRoot = transform.GetChild(2); 
        }

        //Check if the input mode changed
        if(previousAudioInputMode != audioInputMode)
        {
            if(audioInputMode == AudioInputMode.Microphone)
            {
                string microphoneName = Microphone.devices[0];
                Debug.Log("Mic name: " + microphoneName);
                GetComponent<AudioSource>().clip = Microphone.Start(microphoneName, true, 20, AudioSettings.outputSampleRate);
                GetComponent<AudioSource>().Play();
            }
            previousAudioInputMode = audioInputMode; 
        }

        if (deathRow == null)
        {
            deathRow = new List<GameObject>();
        }
        lr = GetComponent<LineRenderer>();

        CreateAndDestroyNecessaryBars();

        //SetBarOrigins();
        UpdateBarOriginTransforms();

        if (barWidth != prevBarWidth)
        {
            foreach(var bar in BarRigidbodiesRoot.GetComponentsInChildren<Transform>())
            {
                if (bar == BarRigidbodiesRoot) //Don't change the scale of the root!
                    continue;
                bar.localScale = new Vector3(barWidth, bar.localScale.y, bar.localScale.z);
            }
        }
    } 

    private void CreateAndDestroyNecessaryBars()
    {
        //Clamp Value from going negative
        if (totalBars < 0) { totalBars = 0; }

        while (BarOriginsRoot.childCount < totalBars)
        {
            GameObject barOrigin = new GameObject("barOrigin" + BarOriginsRoot.childCount);
            barOrigin.transform.localScale = Vector3.one;
            barOrigin.transform.position = Vector3.zero;
            barOrigin.transform.SetParent(BarOriginsRoot);


            GameObject newBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newBar.name = "bar" + BarOriginsRoot.childCount;
            newBar.transform.position = new Vector3(0, 0.5f, 0); // move block so that the edge lines up with the bar origin. 
            newBar.transform.SetParent(BarRigidbodiesRoot);

            newBar.AddComponent<Rigidbody>();
            newBar.GetComponent<Rigidbody>().useGravity = true;

            newBar.AddComponent<ConfigurableJoint>();
            newBar.GetComponent<ConfigurableJoint>().anchor = Vector3.zero;
            newBar.GetComponent<ConfigurableJoint>().autoConfigureConnectedAnchor = false;
            newBar.GetComponent<ConfigurableJoint>().xMotion = ConfigurableJointMotion.Limited;
            newBar.GetComponent<ConfigurableJoint>().yMotion = ConfigurableJointMotion.Locked;
            newBar.GetComponent<ConfigurableJoint>().zMotion = ConfigurableJointMotion.Locked;
            newBar.GetComponent<ConfigurableJoint>().angularXMotion = ConfigurableJointMotion.Locked;
            newBar.GetComponent<ConfigurableJoint>().angularYMotion = ConfigurableJointMotion.Locked;
            newBar.GetComponent<ConfigurableJoint>().angularZMotion = ConfigurableJointMotion.Locked;
            SoftJointLimit sjl = new SoftJointLimit();
            sjl.limit = 3;
            newBar.GetComponent<ConfigurableJoint>().linearLimit = sjl; 


        }
        while (BarOriginsRoot.childCount > totalBars)
        {
            var currBar = BarOriginsRoot.GetChild(BarOriginsRoot.childCount - 1);
            deathRow.Add(currBar.gameObject);
            currBar.SetParent(PurgatoryRoot);
        }
        while (BarRigidbodiesRoot.childCount > totalBars)
        {
            var currBarRb = BarRigidbodiesRoot.GetChild(BarRigidbodiesRoot.childCount - 1);
            deathRow.Add(currBarRb.gameObject);
            currBarRb.SetParent(PurgatoryRoot);
        }
    }

    private void UpdateBarOriginTransforms()
    {
        //Get the total length of the line renderer
        Vector3[] linePoints = new Vector3[lr.positionCount];
        if (linePoints.Length < 2)
            return;
        lr.GetPositions(linePoints);
        float totalLineLength = 0;
        for(int i = 0; i < linePoints.Length - 1; i++)
        {
            totalLineLength += Vector3.Distance(linePoints[i], linePoints[i + 1]); 
        }

        //TODO: Choose which axis the tangent is on

        for(int b = 0; b < BarOriginsRoot.childCount; b++)
        {
            var currBar = BarOriginsRoot.GetChild(b);
            float t = b / (float)BarOriginsRoot.childCount;

            //Set the bar origin to the correct position
            currBar.transform.position = GetPointOnLineRenderer(lr, totalLineLength, t);
            Transform currBarRb = BarRigidbodiesRoot.GetChild(b);
            currBarRb.GetComponent<ConfigurableJoint>().connectedAnchor = currBar.transform.position;

            //and rotate it based on the normal to the line at that point
            Vector3 normal = GetNormalOnLineRenderer(lr, totalLineLength, t);
            Debug.DrawRay(currBar.transform.position, normal, Color.green, 15);

            currBarRb.GetComponent<ConfigurableJoint>().axis = Vector3.up; //normal;
            currBar.transform.LookAt(currBar.transform.position - normal, Vector3.up);
            currBar.transform.Rotate(Vector3.right, 90, Space.Self);

            currBarRb.GetComponent<ConfigurableJoint>().angularXMotion = ConfigurableJointMotion.Free;
            currBarRb.GetComponent<ConfigurableJoint>().angularYMotion = ConfigurableJointMotion.Free;
            currBarRb.GetComponent<ConfigurableJoint>().angularZMotion = ConfigurableJointMotion.Free;
            currBarRb.transform.rotation = currBar.transform.rotation;
            currBarRb.GetComponent<ConfigurableJoint>().angularXMotion = ConfigurableJointMotion.Locked;
            currBarRb.GetComponent<ConfigurableJoint>().angularYMotion = ConfigurableJointMotion.Locked;
            currBarRb.GetComponent<ConfigurableJoint>().angularZMotion = ConfigurableJointMotion.Locked;
        }
    }
    private void InitializeBarRigidbody()
    {

    }


    private Vector3 GetNormalOnLineRenderer(LineRenderer lineRenderer, float totalLineLength, float fraction)
    {
        Vector3 pointOnLine = GetPointOnLineRenderer(lineRenderer, totalLineLength, fraction);
        int segmentIndex = GetSegmentIndex(lineRenderer, fraction);
        Vector3 tangent = lineRenderer.GetPosition(segmentIndex + 1) - lineRenderer.GetPosition(segmentIndex);
        Vector3 normal = Vector3.Cross(tangent, Camera.main.transform.forward).normalized;
        return normal;
    }

    private int GetSegmentIndex(LineRenderer lineRenderer, float fraction)
    {
        float lineLength = 0f;
        for (int i = 0; i < lineRenderer.positionCount - 1; i++)
        {
            lineLength += Vector3.Distance(lineRenderer.GetPosition(i), lineRenderer.GetPosition(i + 1));
        }

        float targetLength = lineLength * fraction;

        float currentLength = 0f;
        for (int i = 0; i < lineRenderer.positionCount - 1; i++)
        {
            float segmentLength = Vector3.Distance(lineRenderer.GetPosition(i), lineRenderer.GetPosition(i + 1));
            if (currentLength + segmentLength >= targetLength)
            {
                return i;
            }
            currentLength += segmentLength;
        }

        return lineRenderer.positionCount - 2;
    }

    public static Vector3 GetPointOnLineRenderer(LineRenderer lineRenderer, float totalLineLength, float fraction)
    {

        float targetLength = totalLineLength * fraction;

        float currentLength = 0f;
        for (int i = 0; i < lineRenderer.positionCount - 1; i++)
        {
            float segmentLength = Vector3.Distance(lineRenderer.GetPosition(i), lineRenderer.GetPosition(i + 1));
            if (currentLength + segmentLength >= targetLength)
            {
                float segmentFraction = (targetLength - currentLength) / segmentLength;
                return Vector3.Lerp(lineRenderer.GetPosition(i), lineRenderer.GetPosition(i + 1), segmentFraction);
            }
            currentLength += segmentLength;
        }

        return lineRenderer.GetPosition(lineRenderer.positionCount - 1);
    }

    // Update is called once per frame
    void Update()
    {
        if(updateMode == UpdateMode.UpdateEachFrame)
        {
            StepUpdate();
        }

        while(deathRow.Count > 0)
        {
            DestroyImmediate(deathRow[deathRow.Count - 1]);
            deathRow.RemoveAt(deathRow.Count - 1); 
        }
    }

    public void StepUpdate()
    {
        float[] spectrumData = new float[visualizerSamples];

        if (BarOriginsRoot == null)
            return;

        if (audioInputMode == AudioInputMode.LiveListen || audioInputMode == AudioInputMode.Microphone)
        {
            GetComponent<AudioSource>().GetSpectrumData(spectrumData, 0, fttwindow);
            //Move the bars based on the spectrum data
            for (int i = 0; i < BarOriginsRoot.childCount; i++)
            {
                var currBar = BarOriginsRoot.GetChild(i);
                // t is the percent index of the spectrum data we are sampling. 
                float t = i /(float) BarOriginsRoot.childCount;
                int spectrumIndex = Mathf.FloorToInt(visualizerSamples * t);
                //Debug.Log("spectrumData.length: " + spectrumData.Length + " SpectrumIndex: " + spectrumIndex);
                float newYScale = spectrumData[spectrumIndex];
                currBar.localScale = new Vector3(currBar.localScale.x, 1 + (newYScale * barHeightMultiplier), currBar.localScale.z);
            }
        }


    }
}
