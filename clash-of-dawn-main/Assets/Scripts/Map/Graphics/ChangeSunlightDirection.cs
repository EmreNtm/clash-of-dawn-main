using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeSunlightDirection : MonoBehaviour
{
    Transform track;

	void Start () {
		track = Camera.main?.transform;
		RenderSettings.sun = GetComponent<Light>();
	}

	void LateUpdate () {
		if (track) {
			transform.LookAt (track.position);
		}
	}

}
