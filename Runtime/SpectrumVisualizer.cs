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

    [SerializeField][Range(0.0f, 1.0f)] private float spectrumStartFraction;
    [SerializeField] [Range(0.0f, 1.0f)] private float spectrumEndFraction;

    public enum AudioInputMode { AudioFile, LiveListen, Microphone } 
    [HideInInspector] public AudioInputMode audioInputMode;
    private AudioInputMode previousAudioInputMode;

    public enum UpdateMode { ManualUpdate, UpdateEachFrame }
    [SerializeField] public UpdateMode updateMode;

    private List<GameObject> deathRow = new List<GameObject>();

    [SerializeField] private int totalBars;
    [SerializeField] private float barDepth;
    private float prevBarDepth;
    public PhysicMaterial barPhysicsMat;

    [SerializeField] private float barWidth;
    private float prevBarWidth;

    [SerializeField] private float barMaxHeight;
    [SerializeField] private float PID_Pgain;
    [SerializeField] private float PID_Dgain;

    [SerializeField] private float sliderHeightLimit;
    private float spectrumSampleMaxValue;
    private float spectrumSampleMinValue;


    [SerializeField] private float barHeightMultiplier;


    private LineRenderer lr;
    private Transform BarOriginsRoot;
    private Transform BarRigidbodiesRoot;
    private Transform PurgatoryRoot;

    [SerializeField] private bool testButton;

    public void Start()
    {
        GetMinMaxSampleValue(inputAudio, out spectrumSampleMinValue, out spectrumSampleMaxValue);
        CustomOnValidate(); 
    }

    public void CustomOnValidate()
    {
        // 1. Build the empty gameobject roots in the hierarchy if they don't exist
        ConstructRootsIfMissing();

        // 2. Check if the input mode changed
        if (previousAudioInputMode != audioInputMode)
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

        // 3. Initialize any variables that may be null
        if (deathRow == null) { deathRow = new List<GameObject>();}
        lr = GetComponent<LineRenderer>();

        // 4. Create or destroy necessary bars and their children
        CreateAndDestroyNecessaryBars();

        // 5. Set the bar origin transform correctly based on the line normal
        UpdateBarOriginTransforms();

        // 6. Update the config joints to match this new axis
        UpdateBars();

        // 7. If the bar width was changed, update it
        if (barDepth != prevBarDepth || prevBarWidth != barWidth)
        {
            foreach(var bar in BarRigidbodiesRoot.GetComponentsInChildren<Transform>())
            {
                if (bar == BarRigidbodiesRoot) //Don't change the scale of the root!
                    continue;
                bar.localScale = new Vector3(barDepth, bar.localScale.y, barWidth);
            }
        }
    } 

    private void UpdateBars()
    {
        foreach(var barRb in BarRigidbodiesRoot.GetComponentsInChildren<BarController>())
        {
            barRb.UpdateConfigJoint(sliderHeightLimit);
            barRb.UpdateBarScale();
        }
    }

    private void ConstructRootsIfMissing()
    {

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
    }

    private void CreateAndDestroyNecessaryBars()
    {
        //Clamp Value from going negative
        if (totalBars < 0) { totalBars = 0; }

        while (BarOriginsRoot.childCount < totalBars)
        {
            int currIndex = BarOriginsRoot.childCount; 
            GameObject barOrigin = new GameObject("barOrigin" + currIndex);
            barOrigin.transform.localScale = Vector3.one;
            barOrigin.transform.position = Vector3.zero;
            barOrigin.transform.SetParent(BarOriginsRoot);

            GameObject barTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            barTarget.name = ("barTarget" + currIndex); 
            barTarget.transform.localScale = Vector3.one;
            barTarget.transform.position = Vector3.zero;
            barTarget.GetComponent<SphereCollider>();
            barTarget.GetComponent<MeshRenderer>().enabled = false; //TODO add inspector toggle for this
            barTarget.transform.SetParent(barOrigin.transform);
            DestroyImmediate(barTarget.GetComponent<SphereCollider>()); 

            GameObject newBarRB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newBarRB.name = "bar" + currIndex;
            newBarRB.transform.position = new Vector3(0, 0.5f, 0); // move block so that the edge lines up with the bar origin. 
            newBarRB.transform.SetParent(BarRigidbodiesRoot);
            newBarRB.transform.rotation = Quaternion.Euler(0, 0, 0);

            newBarRB.AddComponent<BarController>(); //Automatically adds rigidbody and config joint as well, so must run this first
            newBarRB.GetComponent<BarController>().InitBar(barTarget.transform, barOrigin.transform, PID_Pgain, PID_Dgain);

            newBarRB.GetComponent<Rigidbody>().useGravity = true;
            newBarRB.GetComponent<Rigidbody>().drag = 20;
            newBarRB.GetComponent<BoxCollider>().sharedMaterial = barPhysicsMat; 
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
            var currBarOrigin = BarOriginsRoot.GetChild(b);
            Transform currBarRb = BarRigidbodiesRoot.GetChild(b);

            float t = b / (float)BarOriginsRoot.childCount;

            //Set the bar origin to the correct position
            currBarOrigin.transform.position = GetPointOnLineRenderer(lr, totalLineLength, t);

            //and rotate it based on the normal to the line at that point
            Vector3 normal = GetNormalOnLineRenderer(lr, totalLineLength, t);
            Debug.DrawRay(currBarOrigin.transform.position, -normal, Color.green, 1);

            currBarOrigin.transform.LookAt(currBarOrigin.transform.position - normal, Vector3.up);
            currBarOrigin.transform.Rotate(Vector3.right, 90, Space.Self);
        }
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

        UpdateBars(); //TODO seems like a waste to do this every time, but may be necessary to prevent axis from not correctly being set.

        if (audioInputMode == AudioInputMode.LiveListen || audioInputMode == AudioInputMode.Microphone)
        {
            GetComponent<AudioSource>().GetSpectrumData(spectrumData, 0, fttwindow);

            //Trim the edges of the spectrum data as desired
            int startIndex = (int)(spectrumStartFraction * spectrumData.Length);
            int stopIndex = (int)(spectrumEndFraction * spectrumData.Length);

            float[] spectrumSubset = new float[stopIndex - startIndex];
            System.Array.Copy(spectrumData, startIndex, spectrumSubset, 0, spectrumSubset.Length);

            //Move the bars based on the spectrum data
            for (int i = 0; i < BarOriginsRoot.childCount; i++)
            {
                var currOrigin = BarOriginsRoot.GetChild(i);
                // t is the percent index of the spectrum data we are sampling. 
                float t = i /(float)BarRigidbodiesRoot.childCount;
                int spectrumIndex = Mathf.FloorToInt(spectrumSubset.Length * t);
                //Debug.Log("spectrumData.length: " + spectrumData.Length + " SpectrumIndex: " + spectrumIndex);
                float spectrumSample = spectrumData[spectrumIndex];

                Debug.DrawRay(currOrigin.position, currOrigin.up * barMaxHeight, Color.white, 1);
                currOrigin.GetChild(0).transform.position = currOrigin.position + (currOrigin.up * barMaxHeight * Mathf.Clamp01(spectrumSample * barHeightMultiplier)) / 2;

            }
        }

    }
    public static void GetMinMaxSampleValue(AudioClip clip, out float min, out float max)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        min = float.MaxValue;
        max = float.MinValue;

        for (int i = 0; i < samples.Length; i++)
        {
            float sampleValue = Mathf.Abs(samples[i]);

            if (sampleValue < min)
            {
                min = sampleValue;
            }

            if (sampleValue > max)
            {
                max = sampleValue;
            }
        }
        Debug.LogError("min: " + min + " max: " + max); 
    }
}