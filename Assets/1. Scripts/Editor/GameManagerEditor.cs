#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play 모드에서 State Authority(호스트)가 Playing/WaitSync일 때 사용하세요.", MessageType.Info);
            GUI.enabled = false;
        }

        if (GUILayout.Button("Force End Stage (1★ Score + Save Progress)"))
        {
            foreach (var t in targets)
            {
                if (t is GameManager gm)
                    gm.DebugForceEndStageWithOneStar();
            }
        }

        GUI.enabled = true;
    }
}
#endif
