using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    private Action<Explosion> _killAction;
    public bool isExploading = false;
    
    public void Init(Action<Explosion> killAction)
    {
        _killAction = killAction;
    }
    void Update()
    {
        StartCoroutine(Exploading());
    }

    private IEnumerator Exploading()
    {
        isExploading= true;
        yield return new WaitForSeconds(1.5f);
        _killAction(this);
        isExploading= false;
    }
}
