using UnityEngine;

public class WalkStepParticles_Rigidbody : MonoBehaviour
{
    [Header("Particles")]
    public ParticleSystem leftParticle;
    public ParticleSystem rightParticle;

    [Header("References")]
    public Rigidbody playerRigidbody;

    [Header("Step Detection")]
    public float minSpeedThreshold = 0.1f;    // velocit� minima per far partire i passi
    public float baseStepFrequency = 1.5f;    // frequenza dei passi a velocit� di riferimento

    private bool leftStepNext = false;        // inizia con piede destro
    private float stepTimer = 0f;

    private void Update()
    {
        if (playerRigidbody == null) return;

        // Velocit� orizzontale reale
        Vector3 horizontalVelocity = new Vector3(playerRigidbody.linearVelocity.x, 0f, playerRigidbody.linearVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // Se il player non si muove, reset del timer
        if (currentSpeed < minSpeedThreshold)
        {
            stepTimer = 0f;
            return;
        }

        // Frequenza adattiva basata sulla velocit� reale
        float speedRatio = currentSpeed / 3f; // 3f = riferimento per walk speed base
        float adjustedFrequency = baseStepFrequency * speedRatio;

        // Incrementa timer
        stepTimer += Time.deltaTime * adjustedFrequency;

        if (stepTimer >= 1f)
        {
            EmitStep();
            stepTimer = 0f;
        }
    }

    private void EmitStep()
    {
        if (leftStepNext)
        {
            leftParticle?.Emit(1);
        }
        else
        {
            rightParticle?.Emit(1);
        }

        leftStepNext = !leftStepNext; // alterna i passi
    }
}
