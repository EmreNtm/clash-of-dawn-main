using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsView : View
{
    [SerializeField]
    private Button backButton;

    public override void Initialize()
    {
        backButton.onClick.AddListener(() => {
            ViewManager.Instance.Show<MainView>();
        });

        base.Initialize();
    }
}
