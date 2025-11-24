using UnityEngine;

public class WalkStepParticles_Rigidbody : MonoBehaviour
{
    [Header("Particles")]
    public ParticleSystem leftParticle;
    public ParticleSystem rightParticle;

    [Header("References")]
    public Rigidbody playerRigidbody;

    [Header("Step Detection")]
    public float minSpeedThreshold = 0.1f;
    public float baseStepFrequency = 1.5f;

    [Header("Need To Play?")]
    public bool _toPlay;

    private bool leftStepNext = false;
    private float stepTimer = 0f;

    // ================================
    //      FUNZIONI RICHIESTE
    // ================================

    public void EnableToPlay()
    {
        _toPlay = true;
    }

    public void DisableToPlay()
    {
        _toPlay = false;
    }

    // (Se vuoi posso aggiungere anche una ToggleToPlay())

    private void Update()
    {
        if (_toPlay)
        {
            if (playerRigidbody == null) return;

            Vector3 horizontalVelocity = new Vector3(playerRigidbody.linearVelocity.x, 0f, playerRigidbody.linearVelocity.z);
            float currentSpeed = horizontalVelocity.magnitude;

            if (currentSpeed < minSpeedThreshold)
            {
                stepTimer = 0f;
                return;
            }

            float speedRatio = currentSpeed / 3f;
            float adjustedFrequency = baseStepFrequency * speedRatio;

            stepTimer += Time.deltaTime * adjustedFrequency;

            if (stepTimer >= 1f)
            {
                EmitStep();
                stepTimer = 0f;
            }
        }
        else return;
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

        leftStepNext = !leftStepNext;
    }
}
