using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(SpectrumVisualizer))]
public class SpectrumVisualizerInspector : Editor
{
    SerializedProperty audioInputMode;
    SerializedProperty inputAudio;


    private void OnEnable()
    {
        inputAudio = serializedObject.FindProperty("inputAudio");
        audioInputMode = serializedObject.FindProperty("audioInputMode");
    }

    public override void OnInspectorGUI()
    {

        EditorGUI.BeginChangeCheck();

        base.OnInspectorGUI();

        if (EditorGUI.EndChangeCheck())
        {
            var script = target as SpectrumVisualizer;
            script.CustomOnValidate();
        }
    
        GUILayout.Label("This is a Label in a Custom Editor");

        var sv = (SpectrumVisualizer)target;
        EditorGUILayout.PropertyField(audioInputMode);


        //If we are in audio input mode, display the audio input field
        if((int)SpectrumVisualizer.AudioInputMode.AudioFile == audioInputMode.enumValueIndex)
        {
            sv.inputAudio = (AudioClip)EditorGUILayout.ObjectField("Input Audio", inputAudio.objectReferenceValue, typeof(AudioClip), true);
        }


        serializedObject.ApplyModifiedProperties();

        base.OnInspectorGUI();
    }
}
