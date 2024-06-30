using System;
using UnityEngine.EventSystems;

namespace ModCommonUI.Event;

public class MyEventSys : EventSystem
{
    [NonSerialized] public StandaloneInputModule InputModule = null!;

    protected override void Awake()
    {
        
        gameObject.AddComponent<BaseInput>();
        InputModule = gameObject.AddComponent<StandaloneInputModule>();
        InputModule.forceModuleActive = true;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        InputModule.ActivateModule();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        enabled = InputModule;
    }

    protected override void Update()
    {
        if (current == this)
        {
            base.Update();
        }
        else
        {
            InputModule.UpdateModule();
            InputModule.Process();
        }
    }
}