using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class EventSettings : ScriptableObject
{

    public float eventCheckInterval = 60f;

    [System.Serializable]
    public struct AsteroidEventSetting {
        [Range(0f, 1f)]
        public float eventChance;
        public float eventCooldown;
        public float eventStartingCooldown;
        public float eventInvolvedPlayersRadius;
        public GameObject eventPrefab;

        public GameObject asteroidPrefab;
        public Vector2 minMaxAsteroidSize;
        public int asteroidAmount;
        public float borderRadius;
        public float borderThickness;
        public float duration;
    }
    public AsteroidEventSetting asteroidEventSetting;

    [System.Serializable]
    public struct EnemyShipEventSetting {
        [Range(0f, 1f)]
        public float eventChance;
        public float eventCooldown;
        public float eventStartingCooldown;
        public float eventInvolvedPlayersRadius;
        public GameObject eventPrefab;

        public GameObject enemyShipPrefab;
        public Vector2 minMaxEnemyAmount;
        public float borderRadius;
        public float borderThickness;
        public float duration;
    }
    public EnemyShipEventSetting enemyShipEventSetting;

}
