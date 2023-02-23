using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer))]
public class SpectrumVisualizer : MonoBehaviour
{

    [SerializeField] private bool physicsEnabled; 

    [HideInInspector] public AudioClip inputAudio;
    [Space(15), Range(64, 8192)]

    [SerializeField] public int visualizerSimples = 64;
    [SerializeField] public FFTWindow fttwindow;

    public enum AudioInputMode { AudioFile, LiveListen, Microphone } 
    [HideInInspector] public AudioInputMode audioInputMode;

    public enum UpdateMode { ManualUpdate, UpdateEachFrame }
    [SerializeField] public UpdateMode updateMode;

    private List<GameObject> deathRow = new List<GameObject>();

    [SerializeField] private int totalBars;
    [SerializeField] private float barWidth;
    private float prevBarWidth;

    private LineRenderer lr;
    private Transform BarsRoot;
    private Transform PurgatoryRoot;

    private void OnValidate()
    {
        if(transform.childCount <= 0 || transform.GetChild(0).name != "BarsRoot")
        {
            GameObject BarsRootObj = new GameObject("BarsRoot");
            BarsRootObj.transform.position = Vector3.zero;
            BarsRoot = BarsRootObj.transform;
            BarsRoot.SetParent(transform);
        }
        if (transform.childCount <= 1 || transform.GetChild(1).name != "PurgatoryRoot")
        {
            GameObject BarsRootObj = new GameObject("PurgatoryRoot");
            BarsRootObj.transform.position = Vector3.zero;
            BarsRoot = BarsRootObj.transform;
            BarsRoot.SetParent(transform);
        }

        if (deathRow == null)
        {
            deathRow = new List<GameObject>();
        }
        lr = GetComponent<LineRenderer>();

        CreateAndDestroyNecessaryBars();

        SetBarOrigins();

        if (barWidth != prevBarWidth)
        {
            foreach(var bar in BarsRoot.GetComponentsInChildren<Transform>())
            {
                bar.localScale = new Vector3(barWidth, bar.localScale.y, bar.localScale.z);
            }
        }
    }

    private void CreateAndDestroyNecessaryBars()
    {
        //Clamp Value from going negative
        if (totalBars < 0) { totalBars = 0; }

        while (BarsRoot.childCount < totalBars)
        {
            GameObject newBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newBar.transform.SetParent(BarsRoot);
        }
        while (BarsRoot.childCount > totalBars)
        {
            var currBar = BarsRoot.GetChild(BarsRoot.childCount - 1);
            deathRow.Add(currBar.gameObject);
            currBar.SetParent(PurgatoryRoot);
        }
    }

    private void SetBarOrigins()
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

        for(int b = 0; b < BarsRoot.childCount; b++)
        {
            var currBar = BarsRoot.GetChild(b);
            float t = b / (float)BarsRoot.childCount;

            //Set the bar origin to the correct position
            currBar.transform.position = GetPointOnLineRenderer(lr, totalLineLength, t);

            //and rotate it based on the normal to the line at that point
            Vector3 normal = GetNormalOnLineRenderer(lr, totalLineLength, t);
            Debug.DrawRay(currBar.transform.position, normal, Color.green, 15);

            currBar.transform.LookAt(currBar.transform.position + normal);
            currBar.transform.Rotate(Vector3.right, 90, Space.Self);
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
        if(audioInputMode == AudioInputMode.LiveListen)
        {
            //audioSource.GetSpectrumData(spectrumData, 0, fttwindow);
        }
        if (BarsRoot == null)
            return;
        for (int i = 0; i < BarsRoot.childCount; i++)
        {

        }
    }
}
