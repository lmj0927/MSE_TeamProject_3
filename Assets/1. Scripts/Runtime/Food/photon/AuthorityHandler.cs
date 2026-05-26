using Fusion;
using UnityEngine;

public class AuthorityHandler : NetworkBehaviour, IStateAuthorityChanged
{
    bool isAuthorizing;
    System.Action onAuthorized;
    System.Action onNotAuthorized;

    private string Tag => $"[Auth/{name}/P{Runner.LocalPlayer.PlayerId}]";

    public void RequestStateAuthority(System.Action onAuthorized, System.Action onNotAuthorized)
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

    // rpc info is info of the sender. Runs only on the current state authority (target).
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_RequestStateAuthority([RpcTarget] PlayerRef player, RpcInfo info = default)
    {
        Debug.Log($"{Tag} RPC_RequestStateAuthority received. HasAuth={Object.HasStateAuthority} isAuthorizing={isAuthorizing} IsInvokeLocal={info.IsInvokeLocal} src=P{info.Source.PlayerId}");

        if(Object.HasStateAuthority && !isAuthorizing)
        {
            Debug.Log($"{Tag} Granting — setting isAuthorizing=true, sending RPC_Authorized to P{info.Source.PlayerId}");
            isAuthorizing = true;
            RPC_Authorized(info.Source); // call on the requester
        }
        else // either we don't have authority anymore, or already giving it away
        {
            Debug.LogWarning($"{Tag} Cannot grant — HasAuth={Object.HasStateAuthority} isAuthorizing={isAuthorizing}. Sending Rpc_NotAuthorized to P{info.Source.PlayerId}.");
            Rpc_NotAuthorized(info.Source);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_Authorized([RpcTarget] PlayerRef player)
    {
        Debug.Log($"{Tag} RPC_Authorized received. Calling Object.RequestStateAuthority(). HasAuth(before)={HasStateAuthority}");
        Object.RequestStateAuthority(); // request and the result will be executed on StateAuthorityChanged
    }


	[Rpc(RpcSources.All, RpcTargets.All)]
	private void Rpc_NotAuthorized([RpcTarget] PlayerRef player)
	{
        Debug.LogWarning($"{Tag} Rpc_NotAuthorized received. Firing onNotAuthorized. onAuthorized was {(onAuthorized==null ? "null" : "set")}.");
		onNotAuthorized?.Invoke();
		onAuthorized = null;
		onNotAuthorized = null;
	}


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