using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FishNet;
using TMPro;

public class CreateGameView : View
{
    [SerializeField]
    private Button startGameButton;
    [SerializeField]
    private Button backButton;

    [SerializeField]
    private Button toggleReadyButton;
    [SerializeField]
    private TextMeshProUGUI toggleReadyButtonText;

    [SerializeField]
    private TMP_InputField usernameField;
    [SerializeField]
    private TMP_InputField seedField;

    [SerializeField]
    private GameObject content;

    private int seed = 0;

    public override void Initialize()
    {
        usernameField.onValueChanged.AddListener((value) => {
            PlayerData.Instance.username = value;
        });

        backButton.onClick.AddListener(() => {
            if (InstanceFinder.IsServer) {
                InstanceFinder.ServerManager.StopConnection(true);
            } else if (InstanceFinder.IsClient) {
                InstanceFinder.ClientManager.StopConnection();
            }
        });

        toggleReadyButton.onClick.AddListener(() => {
            PlayerData.Instance.ServerSetIsReady(!PlayerData.Instance.isReady);
        });

        if (InstanceFinder.IsHost) {
            startGameButton.onClick.AddListener(() => {
                GameManager.Instance.StartGame(seed);
                ViewManager.Instance.Hide<CreateGameView>();
            });
            startGameButton.gameObject.SetActive(true);

            seedField.onValueChanged.AddListener((value) => {
                seed = int.Parse(value);
            });
            seedField.gameObject.SetActive(true);
        } else {
            startGameButton.gameObject.SetActive(false);
            seedField.gameObject.SetActive(false);
        }

        base.Initialize();
    }

    private void LateUpdate() {
        if (!isInitialized)
            return;

        toggleReadyButtonText.color = PlayerData.Instance.isReady ? Color.green : Color.red;
        startGameButton.interactable = GameManager.Instance.canStart;
        PlayerData pd;
        Transform textTransform;
        for (int i = 0; i < 4; i++) {
            content.transform.GetChild(i).gameObject.SetActive(false);
            if (i < GameManager.Instance.players.Count) {
                pd = GameManager.Instance.players[i];
                textTransform = content.transform.GetChild(i);
                textTransform.gameObject.SetActive(true);
                textTransform.GetComponent<TextMeshProUGUI>().text = pd.username;
                textTransform.GetComponent<TextMeshProUGUI>().color = pd.isReady ? Color.green : Color.red;
            }
        }
    }
}
