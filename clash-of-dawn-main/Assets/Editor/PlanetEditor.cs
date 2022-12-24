using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlanetObject))]
public class PlanetEditor : Editor
{
    PlanetObject planet;
    Editor shapeEditor;
    Editor colorEditor;

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        if (GUILayout.Button("Create Planet")) {
            planet.CreatePlanet();
        }

        DrawSettingsEditor(planet.shapeSettings, planet.OnPlanetShapeUpdate, ref shapeEditor);
        DrawSettingsEditor(planet.colorSettings, planet.OnPlanetColorUpdate, ref colorEditor);
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
        planet = (PlanetObject) target;
    }
}
