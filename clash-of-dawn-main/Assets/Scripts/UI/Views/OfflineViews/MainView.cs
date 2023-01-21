using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainView : View
{
    
    [SerializeField]
    private Button playButton;
    [SerializeField]
    private Button settingsButton;
    [SerializeField]
    private Button storyButton;
    [SerializeField]
    private Button exitButton;

    public override void Initialize()
    {
        playButton.onClick.AddListener(() => {
            ViewManager.Instance.Show<PlayView>();
        });

        settingsButton.onClick.AddListener(() => {
            ViewManager.Instance.Show<SettingsView>();
        });

        storyButton.onClick.AddListener(() => {
            ViewManager.Instance.Show<StoryView>();
        });

        exitButton.onClick.AddListener(() => {
            Application.Quit();
        });

        base.Initialize();
    }

}
