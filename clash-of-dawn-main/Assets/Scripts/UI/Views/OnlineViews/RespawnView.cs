using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RespawnView : View
{
    
    [SerializeField]
    private Button respawnButton;

    public override void Initialize()
    {
        respawnButton.onClick.AddListener(() => {
            PlayerData.Instance.Respawn();
            ViewManager.Instance.Hide<RespawnView>();
        });

        base.Initialize();
    }

}
