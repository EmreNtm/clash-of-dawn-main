using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CtfProgressBar : MonoBehaviour
{
    [SerializeField]
    private Image progressBarSprite;

    private Camera cam;

    private void Start() {
        cam = PlayerData.Instance.playerShip.GetComponentInChildren<Camera>();

        UpdateProgressBar(0f);
    }

    private void Update() {
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
    }

    public void UpdateProgressBar(float progress) {
        progressBarSprite.fillAmount = progress;
    }
}
