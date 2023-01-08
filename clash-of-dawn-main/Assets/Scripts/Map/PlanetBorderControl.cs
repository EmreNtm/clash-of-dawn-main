using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetBorderControl : MonoBehaviour {
    
    private SystemSettings.PlanetSetting planetSetting;
    private float sqrBorderDistance;
    private GameObject playerShip;

    private void Start() {
        planetSetting = GetComponent<PlanetObject>().planetSetting;
        float radius = GetComponent<PlanetObject>().shapeSettings.planetRadius;
        sqrBorderDistance = (radius + planetSetting.borderRadius) * (radius + planetSetting.borderRadius);
    }

    private void FixedUpdate() {
        if (PlayerData.Instance == null || PlayerData.Instance.playerShip == null)
            return;

        playerShip = PlayerData.Instance.playerShip;
        if (Mathf.Abs((playerShip.transform.position - transform.position).magnitude) < (GetComponent<PlanetObject>().shapeSettings.planetRadius + planetSetting.borderRadius)) {
            playerShip.transform.rotation = Quaternion.Inverse(playerShip.transform.rotation);
        }
    }


}
