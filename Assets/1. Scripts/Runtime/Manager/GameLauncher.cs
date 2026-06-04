// owned by YongKyu Lee
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// It handles the start of the Photon connection for the game.
/// </summary>
public class GameLauncher : MonoBehaviour
{
    // Photon-networked lobby scene name
    public const string RootSceneName = "Multi Main Test";

    static GameLauncher _instance;
    public static bool IsRunning => _instance != null;

    NetworkRunner _runner;

    /// <summary>
    /// Local RestAPI server to Photon server
    /// </summary>
    /// <param name="sessionName"></param>
    public static void Launch(string sessionName)
    {
        if (_instance != null)
        {
            Debug.LogWarning("[GameLauncher] Already launching/running. Ignored.");
            return;
        }

        if (string.IsNullOrEmpty(sessionName))
        {
            Debug.LogError("[GameLauncher] sessionName is empty. Abort.");
            return;
        }

        var go = new GameObject("GameLauncher (Runner)");
        _instance = go.AddComponent<GameLauncher>();
        _instance.StartSession(sessionName);
    }

    async void StartSession(string sessionName)
    {
        // don't destroy this until entered into in-game scene.
        DontDestroyOnLoad(gameObject);

        // initialize network runner.
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        int buildIndex = GetSceneIndexByName(RootSceneName);
        if (buildIndex < 0)
        {
            Debug.LogError($"[GameLauncher] Root scene '{RootSceneName}' not found in Build Settings.");
            Cleanup();
            return;
        }

        var sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single);

        // this originally is done by bootstrap
        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        gameObject.AddComponent<NetworkObjectProviderDefault>();

        Debug.Log($"[GameLauncher] StartGame Shared session='{sessionName}' rootScene='{RootSceneName}' (#{buildIndex})");

        // shared start!
        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            Scene = sceneInfo,
            SceneManager = sceneManager,
        });

        if (!result.Ok)
        {
            Debug.LogError($"[GameLauncher] StartGame failed: {result.ShutdownReason}");
            Cleanup();
            return;
        }

        Debug.Log("[GameLauncher] Photon session started.");
    }

    void Cleanup()
    {
        _instance = null;
        Destroy(gameObject);
    }

    /// <summary>
    /// Find scene index
    /// </summary>
    /// <param name="sceneName"></param>
    /// <returns></returns>
    static int GetSceneIndexByName(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.Contains(sceneName + ".unity"))
                return i;
        }

        return -1;
    }
}
