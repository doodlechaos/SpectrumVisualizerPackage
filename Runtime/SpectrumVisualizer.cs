using DSPLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AudioSource))]
public class SpectrumVisualizer : MonoBehaviour
{

    [SerializeField] private bool debugEnabled; 

    [HideInInspector] public AudioClip inputAudioClip;
    //[Space(15), Range(64, 8192)]
    [SerializeField] [HideInInspector] public int visualizerSamples = 64;
    [SerializeField] public FFTWindow fttwindow;

    [SerializeField][Range(0.0f, 1.0f)] private float spectrumStartFraction;
    [SerializeField] [Range(0.0f, 1.0f)] private float spectrumEndFraction;

    public enum AudioInputMode { AudioFile, LiveListen, Microphone } 
    [HideInInspector] public AudioInputMode audioInputMode;
    private AudioInputMode previousAudioInputMode;
    [SerializeField] public float audioFileTime;


    private List<GameObject> deathRow = new List<GameObject>();

    [SerializeField] private int totalBars;
    [SerializeField] private float barDepth;
    private float prevBarDepth;
    public PhysicMaterial barPhysicsMat;

    [SerializeField] private float barWidth;
    private float prevBarWidth;

    [SerializeField] private float barMaxHeight;
    [SerializeField] private float barCapYDepth;

    [SerializeField] private float barTargetLerpFraction;
    [SerializeField] private float BarRigidbodyMass;

    [SerializeField] private float sliderHeightLimit;
    private float spectrumSampleMaxValue;
    private float spectrumSampleMinValue;


    [SerializeField] private float barHeightMultiplier;
    [SerializeField] private Gradient barColorGradient;
    [SerializeField] private float barHeightGlowStrength;

    private LineRenderer lr;
    private Transform BarOriginsRoot;
    private Transform BarStalksRoot;
    private Transform BarRigidbodiesRoot;
    private Transform PurgatoryRoot;

    public int VisualizerLayer;
    public Material stalkMaterial;
    public Material capMaterial;


    public void Start()
    {
        Physics.IgnoreLayerCollision(VisualizerLayer, VisualizerLayer); //Necessary to prevent self collision with config joint 
        GetMinMaxSampleValue(inputAudioClip, out spectrumSampleMinValue, out spectrumSampleMaxValue);
        CustomOnValidate();
        BarRigidbodiesRoot.gameObject.SetActive(true);
        BarStalksRoot.gameObject.SetActive(true);
        if (Application.isPlaying && audioInputMode == AudioInputMode.LiveListen)
        {
            GetComponent<AudioSource>().Play();
        }
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
            }
            if(audioInputMode == AudioInputMode.LiveListen)
            {
                GetComponent<AudioSource>().clip = inputAudioClip; 
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

        // 7. Set the initial bar positions
        foreach(var barStalk in BarStalksRoot.GetComponentsInChildren<BarController>())
        {
            barStalk.SetInitialPosition();

        }

        // 7. If the bar width was changed, update it
        if (barDepth != prevBarDepth || prevBarWidth != barWidth)
        {
            foreach(var bar in BarRigidbodiesRoot.GetComponentsInChildren<Transform>())
            {
                if (bar == BarRigidbodiesRoot) //Don't change the scale of the root!
                    continue;
                bar.localScale = new Vector3(barWidth, barCapYDepth, barDepth);
            }
            foreach (var stalk in BarStalksRoot.GetComponentsInChildren<Transform>())
            {
                if (stalk == BarStalksRoot) //Don't change the scale of the root!
                    continue;
                stalk.localScale = new Vector3(barWidth, stalk.localScale.y, barDepth);
            }
        }

        // Make sure that all the gameobjects are set to the visualizer layer for no self collision:
        SetLayerRecursively(gameObject, VisualizerLayer);
        if (!Application.isPlaying)
        {
            BarRigidbodiesRoot.gameObject.SetActive(false);
            BarStalksRoot.gameObject.SetActive(false);
        }

        if (!Application.isPlaying)
            return; 
        //Color all the bars in a spectrum, using temp materials because if not, it causes a memory leak when done in the editor
        for(int i = 0; i < BarRigidbodiesRoot.childCount; i++)
        {
            var tempMaterial = new Material(BarRigidbodiesRoot.GetChild(i).GetComponent<Renderer>().sharedMaterial);
            tempMaterial.color = barColorGradient.Evaluate(i / (float)BarRigidbodiesRoot.childCount);
            BarRigidbodiesRoot.GetChild(i).GetComponent<Renderer>().sharedMaterial = tempMaterial;

            var tempMaterial2 = new Material(BarStalksRoot.GetChild(i).GetComponent<Renderer>().sharedMaterial);
            tempMaterial2.color = barColorGradient.Evaluate(i / (float)BarRigidbodiesRoot.childCount);
            BarStalksRoot.GetChild(i).GetComponent<Renderer>().sharedMaterial = tempMaterial;
        }
    }



    private void UpdateBars()
    {
        //Update the rigidbodies
        foreach(var barRb in BarRigidbodiesRoot.GetComponentsInChildren<BarController>())
        {
            barRb.transform.GetComponent<Rigidbody>().mass = BarRigidbodyMass;
            barRb.transform.rotation = barRb.origin.rotation;

            barRb.UpdateBarStalk(barHeightGlowStrength);
            if (!Application.isPlaying)
                continue; 
            barRb.MoveTowardsTarget();
        }

    }

    private void ConstructRootsIfMissing()
    {

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
        //Build the roots if they don't exist yet
        if (transform.childCount <= 2 || transform.GetChild(2).name != "BarStalksRoot")
        {
            BarStalksRoot = new GameObject("BarStalksRoot").transform;
            BarStalksRoot.position = Vector3.zero;
            BarStalksRoot.localScale = Vector3.one;
            BarStalksRoot.SetParent(transform);
        }
        else
        {
            BarStalksRoot = transform.GetChild(2);
        }
        if (transform.childCount <= 3 || transform.GetChild(3).name != "PurgatoryRoot")
        {
            PurgatoryRoot = new GameObject("PurgatoryRoot").transform;
            PurgatoryRoot.SetParent(transform);
            PurgatoryRoot.transform.position = Vector3.zero;
            PurgatoryRoot.transform.localScale = Vector3.one;
        }
        else
        {
            PurgatoryRoot = transform.GetChild(3);
        }
    }

    private void CreateAndDestroyNecessaryBars()
    {
        //Clamp Value from going negative
        if (totalBars < 0) { totalBars = 0; }

        while (BarOriginsRoot.childCount < totalBars)
        {
            //Create the origin empty
            int currIndex = BarOriginsRoot.childCount; 
            GameObject barOrigin = new GameObject("barOrigin" + currIndex);
            barOrigin.transform.localScale = Vector3.one;
            barOrigin.transform.position = Vector3.zero;
            barOrigin.transform.SetParent(BarOriginsRoot);

            //Create the target 
            GameObject barTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            barTarget.name = ("barTarget" + currIndex); 
            barTarget.transform.localScale = Vector3.one;
            barTarget.transform.position = Vector3.zero;
            barTarget.GetComponent<MeshRenderer>().enabled = false; //TODO add inspector toggle for this
            barTarget.transform.SetParent(barOrigin.transform);
            DestroyImmediate(barTarget.GetComponent<SphereCollider>());

            //Create the stalk of the bar
            GameObject barStalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barStalk.name = ("barStalk" + currIndex);
            barStalk.transform.localScale = Vector3.one;
            barStalk.transform.position = Vector3.zero;
            barStalk.transform.SetParent(BarStalksRoot);

            //Create the rigidbody cap of the bar
            GameObject newBarRB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newBarRB.name = "barRB_" + currIndex;
            newBarRB.transform.SetParent(BarRigidbodiesRoot);
            newBarRB.transform.rotation = Quaternion.Euler(0, 0, 0);



            newBarRB.AddComponent<BarController>(); //Automatically adds rigidbody and config joint as well, so must run this first
            newBarRB.GetComponent<BarController>().InitBar(barTarget.transform, barOrigin.transform, barStalk.transform);

            //Make the stalk and the cap not collide with one another
            Physics.IgnoreCollision(barStalk.GetComponent<Collider>(), newBarRB.GetComponent<Collider>());

            newBarRB.GetComponent<Rigidbody>().useGravity = false;
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
        while (BarStalksRoot.childCount > totalBars)
        {
            var currBarStalk = BarStalksRoot.GetChild(BarStalksRoot.childCount - 1);
            deathRow.Add(currBarStalk.gameObject);
            currBarStalk.SetParent(PurgatoryRoot);
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

            float t = b / (float)BarOriginsRoot.childCount;

            //Set the bar origin to the correct position
            currBarOrigin.transform.position = GetPointOnLineRenderer(lr, totalLineLength, t);

            //and rotate it based on the normal to the line at that point
            Vector3 normal = GetNormalOnLineRenderer(lr, totalLineLength, t);

            if(debugEnabled)
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

    // Only for procedural genration cleanup
    void Update()
    {
        while(deathRow.Count > 0)
        {
            DestroyImmediate(deathRow[deathRow.Count - 1]);
            deathRow.RemoveAt(deathRow.Count - 1); 
        }
    }

    public void StepUpdate()
    {

        if (BarOriginsRoot == null)
            return;

        UpdateBars();

        float[] spectrumData = new float[visualizerSamples];


        if (audioInputMode == AudioInputMode.LiveListen || audioInputMode == AudioInputMode.Microphone)
        {
            GetComponent<AudioSource>().GetSpectrumData(spectrumData, 0, fttwindow);
        }
        else if (audioInputMode == AudioInputMode.AudioFile)
        {
            inputAudioClip.GetData(spectrumData, Mathf.RoundToInt(inputAudioClip.frequency * audioFileTime));

            FFT fft = new FFT();
            fft.Initialize((uint)spectrumData.Length);

            // Apply our chosen FFT Window
            double[] windowCoefs = DSP.Window.Coefficients(DSP.Window.Type.BH92, (uint)spectrumData.Length);
            double[] scaledSpectrumChunk = DSP.Math.Multiply(Array.ConvertAll(spectrumData, x => (double)x), windowCoefs);
            double scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefs);

            System.Numerics.Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
            double[] scaledFFTSpectrum = DSP.ConvertComplex.ToMagnitude(fftSpectrum);
            scaledFFTSpectrum = DSP.Math.Multiply(scaledFFTSpectrum, scaleFactor);

            for (int i = 0; i < scaledFFTSpectrum.Length; i++)
            {
                scaledFFTSpectrum[i] *= 1; // TODO bonusScaleMyFFT;
            }

            double[] halfFFTout = new double[scaledFFTSpectrum.Length];
            Array.Copy(scaledFFTSpectrum, halfFFTout, scaledFFTSpectrum.Length / 2);

            // These 1024 magnitude values correspond (roughly) to a single point in the audio timeline
            spectrumData = ConvertToFloatArray(scaledFFTSpectrum); 
            
        }

        //For both input modes, we must map to the bars

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
            float t = i / (float)BarRigidbodiesRoot.childCount;
            int spectrumIndex = Mathf.FloorToInt(spectrumSubset.Length * t);
            //Debug.Log("spectrumData.length: " + spectrumData.Length + " SpectrumIndex: " + spectrumIndex);
            float spectrumSample = spectrumData[spectrumIndex];

            if (debugEnabled)
                Debug.DrawRay(currOrigin.position, currOrigin.up * barMaxHeight, Color.white, 1);

            Transform currBarTarget = currOrigin.GetChild(0).transform;
            Vector3 targetIdealPosition = currOrigin.position + (currOrigin.up * barMaxHeight * Mathf.Clamp01(spectrumSample * barHeightMultiplier)) / 2;
            currBarTarget.position = Vector3.Lerp(currBarTarget.position, targetIdealPosition, barTargetLerpFraction);
        }

    }

    private float[] ConvertToFloatArray(double[] arr)
    {
        float[] farr = new float[arr.Length];
        for(int i = 0; i < arr.Length; i++)
        {
            farr[i] = (float)arr[i]; 
        }
        return farr; 
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

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    public int getIndexFromTime(float curTime, float clipLength, int numTotalSamples)
    {
        float lengthPerSample = clipLength / (float)numTotalSamples;

        return Mathf.FloorToInt(curTime / lengthPerSample);
    }

    public float getTimeFromIndex(int index, int sampleRate)
    {
        return ((1f / (float)sampleRate) * index);
    }
}
