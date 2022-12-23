using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FishNet;
using UnityEngine.UI;

public sealed class MainView : View
{

    [SerializeField]
    private Button disconnectButton;

    [SerializeField]
    private TextMeshProUGUI infoText;

    [SerializeField]
    private TextMeshProUGUI scoreText;

    [SerializeField]
    private TextMeshProUGUI playerCountText;

    public override void Initialize()
    {
        disconnectButton.onClick.AddListener(() => {
            if (InstanceFinder.IsServer) {
                InstanceFinder.ServerManager.StopConnection(true);
            } else if (InstanceFinder.IsClient) {
                InstanceFinder.ClientManager.StopConnection();
            }
        });

        base.Initialize();
    }

    private void LateUpdate() {
        if (!isInitialized)
            return;

        infoText.text = $"Is Server = {InstanceFinder.IsServer}, Is Client = {InstanceFinder.IsClient}, Is Host = {InstanceFinder.IsHost}";
        scoreText.text = $"Score = {PlayerData.Instance.score} Points";

        if (InstanceFinder.IsHost) {
            playerCountText.text = $"Players = {InstanceFinder.ServerManager.Clients.Count}";

            playerCountText.gameObject.SetActive(true);
        } else {
            playerCountText.gameObject.SetActive(false);
        }
        
    }

}
