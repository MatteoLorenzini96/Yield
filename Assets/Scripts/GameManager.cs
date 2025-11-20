using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Energy Transfer Settings")]
    public float transferAmount = 0.1f;
    public float npcMultiplier = 5f;
    public float playerReturnFraction = 0.05f;
    public float transferDuration = 1f;

    private float logTimer = 0f;
    public float logInterval = 1f; // stampa una volta al secondo

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        logTimer += Time.deltaTime;

        if (logTimer >= logInterval)
        {
            Debug.Log(
                $"[GameManager Params]\n" +
                $"TransferAmount: {transferAmount}\n" +
                $"NPC Multiplier: {npcMultiplier}\n" +
                $"Player Return Fraction: {playerReturnFraction}\n" +
                $"Transfer Duration: {transferDuration}"
            );

            logTimer = 0f;
        }
    }

    // -----------------------------------------
    // FUNCTIONS TO CHANGE VALUES RUNTIME
    // -----------------------------------------

    public void SetTransferAmount(float value)
    {
        transferAmount = Mathf.Max(0f, value);
    }

    public void SetNpcMultiplier(float value)
    {
        npcMultiplier = Mathf.Max(0f, value);
    }

    public void SetPlayerReturnFraction(float value)
    {
        playerReturnFraction = Mathf.Clamp01(value);
    }

    public void SetTransferDuration(float value)
    {
        transferDuration = Mathf.Max(0.01f, value);
    }

    // Set all parameters at once
    public void SetEnergyTransferParameters(
        float transfer,
        float multiplier,
        float returnFrac,
        float duration)
    {
        SetTransferAmount(transfer);
        SetNpcMultiplier(multiplier);
        SetPlayerReturnFraction(returnFrac);
        SetTransferDuration(duration);
    }
}
