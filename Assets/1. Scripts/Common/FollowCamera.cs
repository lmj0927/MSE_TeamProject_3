// Owned by MinJun Lee
using UnityEngine;

/// <summary>
/// Follows target on Y and Z axis.
/// </summary>
public class FollowCamera : MonoBehaviour
{
    public Transform target; // follow target
    [SerializeField] private Vector3 offset; // camera offset

    private void LateUpdate()
    {
        if (target == null) return;
        // lock X to 0, follow target Y and Z only
        transform.position = new Vector3(0, target.position.y, target.position.z) + offset;
    }
}
