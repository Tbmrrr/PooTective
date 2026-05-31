// DoorTrigger.cs 挂在独立的 DoorTriggerZone 物体上
using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public DoorInteractable door; // Inspector 里拖入门的 DoorInteractable

    private void OnTriggerEnter(Collider other)
    {
        if (door != null) door.OnPlayerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (door != null) door.OnPlayerExit(other);
    }
}