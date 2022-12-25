using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapManager))]
public class SystemEditor : Editor
{

    MapManager mapManager;
    Editor systemSettingsEditor;

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        if (GUILayout.Button("Create System")) {
            mapManager.CreateOfflineSystem();
        }

        DrawSettingsEditor(mapManager.systemSettings, mapManager.OnSystemSettingsUpdate, ref systemSettingsEditor);
    }

    private void DrawSettingsEditor(Object settings, System.Action onSettingsUpdated, ref Editor editor) {
        if (settings == null)
            return;

        using (var check = new EditorGUI.ChangeCheckScope()) {
            CreateCachedEditor(settings, null, ref editor);
            editor.OnInspectorGUI();

            if (check.changed && onSettingsUpdated != null) {
                onSettingsUpdated();
            }
        }
    }

    private void OnEnable() {
        mapManager = (MapManager) target;
    }

}
