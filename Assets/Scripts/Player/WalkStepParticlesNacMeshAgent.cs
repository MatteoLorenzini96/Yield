using UnityEngine;
using UnityEngine.AI;

public class WalkStepParticles_NavMeshAgent : MonoBehaviour
{
    [Header("Particles")]
    public ParticleSystem leftParticle;
    public ParticleSystem rightParticle;

    [Header("References")]
    // Riferimento al NavMeshAgent
    public NavMeshAgent npcNavMeshAgent;

    [Header("Step Detection")]
    // Velocità minima per far partire i passi
    public float minSpeedThreshold = 0.1f;
    // Frequenza dei passi a velocità di riferimento
    public float baseStepFrequency = 1.5f;
    // Velocità di riferimento per il calcolo della frequenza (es. la velocità di camminata standard dell'NPC)
    public float referenceSpeed = 3f;

    [Header("Need To Play?")]
    public bool _toPlay = true; // Impostato a true di default

    private bool leftStepNext = false; // Inizia con piede destro (o sinistro, non importa)
    private float stepTimer = 0f;

    private void Awake()
    {
        // Ottiene il NavMeshAgent se non è assegnato nell'Inspector
        if (npcNavMeshAgent == null)
        {
            npcNavMeshAgent = GetComponent<NavMeshAgent>();
        }

        if (npcNavMeshAgent == null)
        {
            Debug.LogError($"{gameObject.name}: NavMeshAgent non trovato o non assegnato.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (!_toPlay || npcNavMeshAgent == null)
        {
            // Se non deve suonare o l'agente non è disponibile, azzera il timer e esci
            stepTimer = 0f;
            return;
        }

        // Velocità orizzontale reale del NavMeshAgent
        // Utilizziamo la proprietà 'velocity' del NavMeshAgent
        Vector3 horizontalVelocity = new Vector3(npcNavMeshAgent.velocity.x, 0f, npcNavMeshAgent.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // Se l'NPC non si muove (o si muove troppo lentamente), reset del timer
        if (currentSpeed < minSpeedThreshold)
        {
            stepTimer = 0f;
            return;
        }

        // Calcolo della frequenza adattiva basata sulla velocità reale
        // La frequenza aumenta proporzionalmente a quanto la velocità attuale è maggiore 
        // della velocità di riferimento (referenceSpeed)
        float speedRatio = currentSpeed / referenceSpeed;
        // La frequenza aggiustata si basa sulla frequenza base moltiplicata per il rapporto di velocità
        float adjustedFrequency = baseStepFrequency * speedRatio;

        // Incrementa il timer, moltiplicando per la frequenza aggiustata
        // Una frequenza maggiore fa riempire il timer più velocemente.
        stepTimer += Time.deltaTime * adjustedFrequency;

        // Quando il timer raggiunge o supera 1f, è il momento di emettere il passo
        if (stepTimer >= 1f)
        {
            EmitStep();
            stepTimer = 0f; // Reset del timer
        }
    }

    private void EmitStep()
    {
        // Alterna tra le due ParticleSystem per simulare i passi alternati (sinistro/destro)
        if (leftStepNext)
        {
            // La sintassi ?.Emit(1) previene un errore se la ParticleSystem è null
            leftParticle?.Emit(1);
        }
        else
        {
            rightParticle?.Emit(1);
        }

        // Inverte lo stato per il passo successivo
        leftStepNext = !leftStepNext;
    }
}