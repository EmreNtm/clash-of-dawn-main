using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FishNet;
using FishNet.Transporting.Tugboat;

public class PlayView : View
{

    [SerializeField]
    private Button createGameButton;
    [SerializeField]
    private Button joinGameButton;
    [SerializeField]
    private Button backButton;

    public override void Initialize()
    {
        createGameButton.onClick.AddListener(() => {
            InstanceFinder.NetworkManager.gameObject.GetComponent<Tugboat>().SetClientAddress("localhost");
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();
        });

        joinGameButton.onClick.AddListener(() => {
            ViewManager.Instance.Show<JoinGameView>();
        });

        backButton.onClick.AddListener(() => {
            ViewManager.Instance.Show<MainView>();
        });

        base.Initialize();
    }

}
