using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RigidbodyThirdPersonMovement))]
public class PlayerSpeedBoost : MonoBehaviour
{
    [Header("Boost Settings")]
    [SerializeField] private float speedMultiplier = 2f;
    [SerializeField] private float boostDuration = 5f;

    private RigidbodyThirdPersonMovement playerMovement;
    private bool isBoosted = false;

    private void Awake()
    {
        playerMovement = GetComponent<RigidbodyThirdPersonMovement>();
        if (playerMovement == null)
            Debug.LogError("RigidbodyThirdPersonMovement non trovato sul player!");
    }

    public void ActivateBoost()
    {
        if (!isBoosted)
            StartCoroutine(BoostRoutine());
        else
            Debug.Log("Boost già attivo!");
    }

    private IEnumerator BoostRoutine()
    {
        isBoosted = true;
        Debug.Log("⚡ Boost attivato! Velocità aumentata.");

        // Applica il boost usando il modificatore del player
        playerMovement.SpeedBoostModifier = speedMultiplier;

        yield return new WaitForSeconds(boostDuration);

        // Ripristina il modificatore
        playerMovement.SpeedBoostModifier = 1f;
        Debug.Log("🛑 Boost terminato. Velocità ripristinate.");
        isBoosted = false;
    }
}
