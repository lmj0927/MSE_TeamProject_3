using UnityEngine;

public class Stamina : MonoBehaviour
{
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina = 100f;
    [SerializeField] private float drainPerSecond = 20f;
    [SerializeField] private float regenPerSecond = 15f;

    public float Max => maxStamina;
    public float Current => currentStamina;
    public float Normalized => maxStamina <= 0f ? 0f : Mathf.Clamp01(currentStamina / maxStamina);
    public bool HasStamina => currentStamina > 0.001f;

    private void Awake()
    {
        maxStamina = Mathf.Max(0f, maxStamina);
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    public bool TryDrainForRunning(float deltaTime)
    {
        if (deltaTime <= 0f)
            return HasStamina;

        float cost = drainPerSecond * deltaTime;
        if (currentStamina <= 0f || cost <= 0f)
            return HasStamina;

        currentStamina = Mathf.Max(0f, currentStamina - cost);
        return HasStamina;
    }

    public void RegenWhileIdle(float deltaTime)
    {
        if (deltaTime <= 0f || regenPerSecond <= 0f || maxStamina <= 0f)
            return;

        currentStamina = Mathf.Min(maxStamina, currentStamina + regenPerSecond * deltaTime);
    }
}

