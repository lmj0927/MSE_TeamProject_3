using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class TestCounters : MonoBehaviour
{
    [SerializeField] private List<ACounter> counters;
    [SerializeField] private PlayerController player;
    [SerializeField] private NetworkObject food; 

    private void Start()
    {
        player.AddFood(food);
    }

    private void Update()
    {
        if (counters == null || player == null)
        {
            return;
        }

        int maxCheckCount = Mathf.Min(counters.Count, 9);
        for (int i = 0; i < maxCheckCount; i++)
        {
            KeyCode alphaKey = KeyCode.Alpha1 + i;
            KeyCode keypadKey = KeyCode.Keypad1 + i;
            if (!Input.GetKeyDown(alphaKey) && !Input.GetKeyDown(keypadKey))
            {
                continue;
            }

            ACounter targetCounter = counters[i];
            if (targetCounter != null)
            {
                targetCounter.Interact(player);
            }

            break;
        }
    }
}
