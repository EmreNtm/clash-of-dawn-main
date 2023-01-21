using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    
    [SerializeField]
    private GameObject ship;

    [SerializeField]
    private Image healthBarSprite;

    private Camera cam;

    private void Start() {
        if (PlayerData.Instance.playerShip == ship) {
            gameObject.SetActive(false);
            return;
        }

        cam = PlayerData.Instance.playerShip.GetComponentInChildren<Camera>();

        UpdateHealthBar(100f, 100f);
    }

    private void Update() {
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
    }

    public void UpdateHealthBar(float maxHealth, float currentHealth) {
        healthBarSprite.fillAmount = currentHealth / maxHealth;
    }

}
