// owned by Yongkyu Lee
using Fusion;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour, IPlayerJoined
{
    public GameObject PlayerPrefab;

    public override void Spawned()
    {
        if(Runner == null)
        {
            Debug.LogWarning("[PlayerSpawner Start] Runner is null.");
            return;   
        }

        if(Runner.LocalPlayer.IsRealPlayer && Runner.GetPlayerObject(Runner.LocalPlayer) == null)
        {
            Debug.Log("[PlayerSpawner Start] Local player spawned.");
            Spawn(Runner.LocalPlayer);
        }
        else
        {
            Debug.LogWarning("Runner.LocalPlayer.IsRealPlayer " + Runner.LocalPlayer.IsRealPlayer + " Runner.GetPlayerObject(Runner.LocalPlayer) " + Runner.GetPlayerObject(Runner.LocalPlayer));
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            Spawn(player);
        }
    }

    private void Spawn(PlayerRef player)
    {
        NetworkObject p = Runner.Spawn(PlayerPrefab, new Vector3(0.759130239f,0.0104335472f,8.3718853f), Quaternion.identity);
        Runner.SetPlayerObject(player, p);
    }
}