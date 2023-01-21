using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ViewManager : MonoBehaviour
{
    
    public static ViewManager Instance { get; private set; }

    [SerializeField]
    private bool autoInitialize;

    [SerializeField]
    private View[] views;

    [SerializeField]
    private View defaultView;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        if (autoInitialize)
            Initialize();
    }

    public void Initialize() {
        foreach (View view in views) {
            view.Initialize();

            view.Hide();
        }

        if (defaultView != null)
            defaultView.Show();
    }

    public void Show<TView>(object args = null) where TView : View {
        foreach (View view in views) {
            if (view is TView) {
                view.Show(args);
            } else {
                view.Hide();
            }
        }
    }

    public void Hide<TView>() where TView : View {
        foreach (View view in views) {
            if (view is TView) {
                view.Hide();
                return;
            }
        }
    }

}
