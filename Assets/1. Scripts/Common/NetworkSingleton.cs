// Owned by MinJun Lee
using System;
using Fusion;
using UnityEngine;

public abstract class NetworkSingleton<T> : NetworkBehaviour where T : Component
{
    [SerializeField] private bool isDontDestroyOnLoad = false;
    private static T _instance;
    private static bool quitting = false;

    public static T Instance => _instance;

    private static Action initializer;
    public static void BindInitializer(Action i)
    {
        initializer += i;
    }

    public override void Spawned()
    {
        if (_instance == null)
        {
            _instance = this as T;
            if (isDontDestroyOnLoad)
            {
                Runner.MakeDontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            if (_instance != this)
            {
                Runner.Despawn(Object);
                return;
            }
        }

        initializer?.Invoke();
    }
    protected virtual void OnApplicationQuit()
    {
        quitting = true;
    }

    // protected virtual void Initialize()
    // {
    // }
}
