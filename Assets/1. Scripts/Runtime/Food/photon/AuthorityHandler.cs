// owned by Yongkyu Lee
using Fusion;
using UnityEngine;
using System;

/// <summary>
/// It is utility class for easy handling the authority handoff process.
/// </summary>
public class AuthorityHandler : NetworkBehaviour, IStateAuthorityChanged
{
    /// <summary>
    /// It prevent the duplicated authorization.
    /// </summary>
    bool isAuthorizing;
    
    /// <summary>
    /// Callback for authorized state.
    /// </summary>
    Action onAuthorized;

    /// <summary>
    /// Callback for not authorized state.
    /// </summary>
    Action onNotAuthorized;

    /// <summary>
    /// It prevents stealing the state authority (similar to lock).
    /// </summary>
    bool barrier = false;
    
    /// <summary>
    /// Block the new request on state authority.
    /// </summary>
    public void Barrier() => barrier = true;
    /// <summary>
    /// Release the block of requesting state authority.
    /// </summary>
    public void Unbarrier() => barrier = false;

    /// <summary>
    /// Tag for this object. (debug)
    /// </summary>
    private string Tag => $"[Auth/{name}/P{Runner.LocalPlayer.PlayerId}]";

    /// <summary>
    /// Request the state authority.
    /// </summary>
    /// <param name="onAuthorized">Callback for authorizing.</param>
    /// <param name="onNotAuthorized">Callback for not authorizing.</param>
    public void RequestStateAuthority(Action onAuthorized, Action onNotAuthorized)
    {
        Debug.Log($"{Tag} RequestStateAuthority called. HasAuth={HasStateAuthority} isAuthorizing={isAuthorizing} currentAuth=P{Object.StateAuthority.PlayerId}");

        // Fast path: I already have authority — fire immediately, no RPC.
        if(HasStateAuthority)
        {
            Debug.Log($"{Tag} Fast path — local already has authority. Firing onAuthorized.");
            onAuthorized?.Invoke();
            return;
        }

        if(isAuthorizing)
        {
            Debug.LogWarning($"{Tag} REJECTED locally — isAuthorizing already true (request in flight). Firing onNotAuthorized.");
            onNotAuthorized();
            return;
        }

        this.onAuthorized = onAuthorized;
        this.onNotAuthorized = onNotAuthorized;
        // mark requester busy — prevents duplicate requests overwriting the slot
        // while a transfer is in flight. Cleared on: StateAuthorityChanged (gained)
        // or Rpc_NotAuthorized.
        isAuthorizing = true;
        Debug.Log($"{Tag} Set requester isAuthorizing=true. Sending RPC_RequestStateAuthority to P{Object.StateAuthority.PlayerId}");
        RPC_RequestStateAuthority(Object.StateAuthority);
    }

    /// <summary>
    /// Request the state authority to specific player.
    /// </summary>
    /// <param name="player">Player reference specified when calling this method</param>
    /// <param name="info">Rpc info is info of the sender. Runs only on the current state authority (target).</param>
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_RequestStateAuthority([RpcTarget] PlayerRef player, RpcInfo info = default)
    {
        Debug.Log($"{Tag} RPC_RequestStateAuthority received. HasAuth={Object.HasStateAuthority} isAuthorizing={isAuthorizing} IsInvokeLocal={info.IsInvokeLocal} src=P{info.Source.PlayerId}");

        if(barrier)
        {
            Debug.LogWarning($"{Tag} Cannot grant - Barrier is active");
            Rpc_NotAuthorized(info.Source, true);
            return;
        }
        else if(Object.HasStateAuthority && !isAuthorizing)
        {
            
            Debug.Log($"{Tag} Granting — setting isAuthorizing=true, sending RPC_Authorized to P{info.Source.PlayerId}");
            isAuthorizing = true;
            RPC_Authorized(info.Source); // call on the requester
        }
        else // either we don't have authority anymore, or already giving it away
        {
            Debug.LogWarning($"{Tag} Cannot grant — HasAuth={Object.HasStateAuthority} isAuthorizing={isAuthorizing}. Sending Rpc_NotAuthorized to P{info.Source.PlayerId}.");
            Rpc_NotAuthorized(info.Source, false);
        }
    }

    /// <summary>
    /// Callback for requesting player when successed to get authority.
    /// </summary>
    /// <param name="player">In this case, the player is requester.</param>
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_Authorized([RpcTarget] PlayerRef player)
    {
        Debug.Log($"{Tag} RPC_Authorized received. Calling Object.RequestStateAuthority(). HasAuth(before)={HasStateAuthority}");
        Object.RequestStateAuthority(); // request and the result will be executed on StateAuthorityChanged
    }

    /// <summary>
    /// Callback for requesting player when failed to get authority.
    /// </summary>
    /// <param name="player">In this case, the player is requester.</param>
    /// <param name="isBarriered">Whether it fails because of the barrier?</param>
	[Rpc(RpcSources.All, RpcTargets.All)]
	private void Rpc_NotAuthorized([RpcTarget] PlayerRef player, bool isBarriered)
	{
        Debug.LogWarning($"{Tag} Rpc_NotAuthorized received. Firing onNotAuthorized. onAuthorized was {(onAuthorized==null ? "null" : "set")}.");
		onNotAuthorized?.Invoke();
		onAuthorized = null;
		onNotAuthorized = null;
        if(isBarriered) 
        {
            Debug.LogWarning($"{Tag} Cannot grant - Barrier is active");
            isAuthorizing = false;
        }
	}

    /// <summary>
    /// Callback for success of changing state authority.
    /// </summary>
    public void StateAuthorityChanged()
    {
        Debug.Log($"{Tag} StateAuthorityChanged fired. HasAuth={HasStateAuthority} isAuthorizing={isAuthorizing} newAuth=P{Object.StateAuthority.PlayerId}");

        if(isAuthorizing)
        {
            Debug.Log($"{Tag} Clearing isAuthorizing flag.");
            isAuthorizing = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if(onAuthorized != null && HasStateAuthority)
        {
            Debug.Log($"{Tag} FixedUpdateNetwork firing pending onAuthorized (auth arrived via transfer).");
            onAuthorized?.Invoke();
            onAuthorized = null;
            onNotAuthorized = null;
        }
    }
}