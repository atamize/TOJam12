using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Main))]
public class PlayerConsole : Editor
{
    SpewEventCode eventCode = SpewEventCode.JoinRoom;
    string[] commands = new string[6];
    Main main;

    void OnEnable()
    {
        main = (Main)target;

        for (int i = 0; i < commands.Length; ++i)
        {
            commands[i] = "player" + (i + 1);
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Separator();

        eventCode = (SpewEventCode)EditorGUILayout.EnumPopup("Event code", eventCode); 

        for (int i = 0; i < commands.Length; ++i)
        {
            EditorGUILayout.BeginHorizontal();
            commands[i] = EditorGUILayout.TextField("Player " + (i + 1), commands[i]);

            if (GUILayout.Button("Go"))
            {
                main.DebugEvent(eventCode, commands[i], i + 1);
                commands[i] = string.Empty;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Quick Start"))
        {
            main.QuickStart();
        }
    }
}
