using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(SpectrumVisualizer))]
public class SpectrumVisualizerInspector : Editor
{
    //SerializedProperty audioInputMode;
    SerializedProperty inputAudioClip;
    SerializedProperty visualizerSamples;


    private void OnEnable()
    {
        inputAudioClip = serializedObject.FindProperty("inputAudioClip");
        //audioInputMode = serializedObject.FindProperty("audioInputMode");
    }

    public override void OnInspectorGUI()
    {
        var sv = target as SpectrumVisualizer;

        EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);
        sv.inputAudioClip = (AudioClip)EditorGUILayout.ObjectField("Input Audio", inputAudioClip.objectReferenceValue, typeof(AudioClip), true);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Visualizer Samples ");
        sv.visualizerSamples = EditorGUILayout.IntSlider(sv.visualizerSamples, 64, 8192);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Visualizer Bar Settings", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        base.OnInspectorGUI();

        if (EditorGUI.EndChangeCheck())
        {
            sv.CustomOnValidate();
        }
    
        GUILayout.Label("Warning: Simulate Physics BEFORE stepping the visualizer in script to maintain lockstep with stalk and cap");
        serializedObject.ApplyModifiedProperties();


        //var sv = (SpectrumVisualizer)target;
        //EditorGUILayout.PropertyField(audioInputMode);


        //If we are in audio input mode, display the audio input field
        //if((int)SpectrumVisualizer.AudioInputMode.AudioFile == audioInputMode.enumValueIndex)
        //{
        //sv.inputAudioClip = (AudioClip)EditorGUILayout.ObjectField("Input Audio", inputAudio.objectReferenceValue, typeof(AudioClip), true);
        //}



    }
}
