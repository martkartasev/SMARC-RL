using UnityEditor;
using UnityEngine;


namespace BagReplay
{
#if UNITY_EDITOR
    [CustomEditor(typeof(BagReplay))]
    public class BagReplayEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BagReplay replay = (BagReplay)target;

            EditorGUILayout.Space();

            if (GUILayout.Button("Select File"))
            {
                string path = EditorUtility.OpenFilePanel("Select Text File", "", "db3");
                if (!string.IsNullOrEmpty(path))
                {
                    replay.SetDB3Path(path);
                    EditorUtility.SetDirty(replay); // mark scene dirty so changes save
                }
            }

            var bagDataWriter = replay.GetComponent<BagDataWriter>();
            if (bagDataWriter != null)
            {
                if (GUILayout.Button("Write CSV"))
                {
                    replay.ReadFile();
                    bagDataWriter.WriteFile();
                }
            }
        }
    }

#endif
}