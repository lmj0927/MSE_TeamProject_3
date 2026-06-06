// Owned by YongKyu Lee
using System;
using Fusion;
using UnityEngine;

/// <summary>
/// Singleton for NetworkBehavior
/// </summary>
/// <typeparam name="T">Any type to use singleton pattern.</typeparam>
public abstract class NetworkSingleton<T> : NetworkBehaviour where T : Component
{
    /// <summary>
    /// Let this object don't destroy on scene change if it is true.
    /// </summary>
    [SerializeField] private bool isDontDestroyOnLoad = false;

    private static T _instance;
    public static T Instance => _instance;

    /// <summary>
    /// It is called on `Spawned` method.
    /// There are some patterns depending on the Spawned method of singleton managers.
    /// But there are spawning timing gap so that one may fail to initialized by the manager.
    /// It forces the order by register other's initializer into this singleton object.
    /// </summary>
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
        initializer = null;
    }
    // protected virtual void OnApplicationQuit()
    // {
    //     quitting = true;
    // }

    // protected virtual void Initialize()
    // {
    // }
}
