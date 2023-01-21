using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FishNet;

public class EscView : View
{

    [SerializeField]
    private Button resumeButton;
    [SerializeField]
    private Button disconnectButton;
    [SerializeField]
    private Button exitGameButton;

    public override void Initialize()
    {
        resumeButton.onClick.AddListener(() => {
            ViewManager.Instance.Hide<EscView>();
        });

        disconnectButton.onClick.AddListener(() => {
            if (InstanceFinder.IsServer) {
                InstanceFinder.ServerManager.StopConnection(true);
            } else if (InstanceFinder.IsClient) {
                InstanceFinder.ClientManager.StopConnection();
            }
        });

        exitGameButton.onClick.AddListener(() => {
            Application.Quit();
        });

        base.Initialize();
    }

}
