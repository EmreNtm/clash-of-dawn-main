using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;
using FishNet.Transporting.Tugboat;

public class JoinGameView : View
{
    [SerializeField]
    private Button joinGameButton;
    [SerializeField]
    private Button backButton;

    [SerializeField]
    private TMP_InputField ipField;

    private string ip = "localhost";

    public override void Initialize()
    {
        joinGameButton.onClick.AddListener(() => {
            InstanceFinder.NetworkManager.gameObject.GetComponent<Tugboat>().SetClientAddress(ip);
            InstanceFinder.ClientManager.StartConnection();
        });

        backButton.onClick.AddListener(() => {
            ViewManager.Instance.Show<PlayView>();
        });

        ipField.onValueChanged.AddListener((value) => {
            ip = value;
        });

        base.Initialize();
    }
}
