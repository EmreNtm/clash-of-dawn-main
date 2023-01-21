using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;


public class LobbyView : View
{
    
    [SerializeField]
    private Button toggleReadyButton;

    [SerializeField]
    private TextMeshProUGUI toggleReadyButtonText;

    [SerializeField]
    private Button startGameButton;

    [SerializeField]
    private TextMeshProUGUI infoText;

    [SerializeField]
    private TextMeshProUGUI playerCountText;

    public override void Initialize()
    {
        toggleReadyButton.onClick.AddListener(() => {
            PlayerData.Instance.ServerSetIsReady(!PlayerData.Instance.isReady);
        });

        if (InstanceFinder.IsHost) {
            startGameButton.onClick.AddListener(() => {
                //GameManager.Instance.StartGame();
                //ViewManager.Instance.Show<MainView>();
            });

            startGameButton.gameObject.SetActive(true);
        } else {
            startGameButton.gameObject.SetActive(false);
        }

        base.Initialize();
    }

    private void LateUpdate() {
        if (!isInitialized)
            return;

        toggleReadyButtonText.color = PlayerData.Instance.isReady ? Color.green : Color.red;
        startGameButton.interactable = GameManager.Instance.canStart;

        infoText.text = $"Is Server = {InstanceFinder.IsServer}, Is Client = {InstanceFinder.IsClient}, Is Host = {InstanceFinder.IsHost}";

        if (InstanceFinder.IsHost) {
            playerCountText.text = $"Players = {InstanceFinder.ServerManager.Clients.Count}";

            playerCountText.gameObject.SetActive(true);
        } else {
            playerCountText.gameObject.SetActive(false);
        }
    }

}
