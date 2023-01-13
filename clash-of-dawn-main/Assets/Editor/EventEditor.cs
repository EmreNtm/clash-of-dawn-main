using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EventManager))]
public class EventEditor : Editor
{

    EventManager eventManager;
    Editor eventEditor;

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        DrawSettingsEditor(eventManager.eventSettings, null, ref eventEditor);
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
        eventManager = (EventManager) target;
    }

}