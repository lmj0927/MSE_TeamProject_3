// Owned by YongKyu Lee
using Fusion;
using UnityEngine;

public class Food : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(SyncPhysicalState))] private bool isHeld { get; set; }
    public bool IsHeld => isHeld;

    [SerializeField] private FoodSO data;
    public FoodSO Data => data;
    public void SetData(FoodSO data) => this.data = data;
    public void SetHeld() => isHeld = true;
    public void SetDrop() => isHeld = false;

    private ChangeDetector _cd;

    public override void Spawned()
    {
        _cd = GetChangeDetector(ChangeDetector.Source.SimulationState);
        SyncPhysicalState();
    }

    private void SyncPhysicalState()
    {
        if(IsHeld)
        {
            foreach (var rb in GetComponentsInChildren<Rigidbody>())
            {
                if(rb.isKinematic) continue;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;
        }
        else if(!IsHeld)
        {
            foreach (var rb in GetComponentsInChildren<Rigidbody>())
            {
                if(!rb.isKinematic) continue;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = false;
            }
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = true;
        }
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetHeld() => SetHeld();

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetDrop() => SetDrop();
}
