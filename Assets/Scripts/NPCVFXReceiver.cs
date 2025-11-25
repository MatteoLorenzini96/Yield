using UnityEngine;

public class NPCVFXReceiver : MonoBehaviour
{
    [Tooltip("Il ParticleSystem principale (parent) o quello da ruotare.")]
    [SerializeField] public ParticleSystem incomingVFX;

    [Tooltip("Il ParticleSystem child la cui emissione deve essere controllata.")]
    [SerializeField] public ParticleSystem emissionChildVFX;

    [Tooltip("La velocità di rotazione del VFX verso il player.")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Emission Settings")]
    [Tooltip("Il Rate over Time massimo desiderato (Fullness Player = 1).")]
    [SerializeField] private float maxRateOverTime = 6.5f;
    [Tooltip("Il Rate over Time minimo desiderato (Fullness Player = -1).")]
    [SerializeField] private float minRateOverTime = 0.0f;

    private Transform _playerTransform;
    private bool _isVFXActive = false; // Controlla l'attività della rotazione
    public float _currentPlayerFullness = 1f; // Reso pubblico per l'accesso da EnergyTransfer

    // Modulo di emissione per un accesso efficiente ai parametri
    private ParticleSystem.EmissionModule _emissionModule;

    private void Awake()
    {
        if (incomingVFX == null || emissionChildVFX == null)
        {
            Debug.LogError($"{name}: Devi assegnare sia il ParticleSystem principale che quello child per controllare l'emissione!");
            enabled = false;
            return;
        }

        // Ottiene il modulo di emissione dal ParticleSystem child
        _emissionModule = emissionChildVFX.emission;

        // Assicura che entrambi i sistemi di particelle siano fermi all'inizio
        incomingVFX.Stop();
        emissionChildVFX.Stop();

        // Cerca il player (il GameObject che ha EnergyTransfer e tag "Player")
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"{name}: Player non trovato con il tag 'Player'. La rotazione del VFX non funzionerà.");
        }
    }

    private void Update()
    {
        // 🔥 Rotazione continua solo se lo scambio è attivo
        if (_isVFXActive && _playerTransform != null)
        {
            RotateVFXTowardsPlayer();
        }
    }

    /// <summary>
    /// Attiva il flag per iniziare la rotazione nell'Update().
    /// </summary>
    public void StartReceivingVFX()
    {
        _isVFXActive = true;
    }

    /// <summary>
    /// Disattiva il flag per fermare la rotazione nell'Update().
    /// </summary>
    public void StopReceivingVFX()
    {
        _isVFXActive = false;
    }

    /// <summary>
    /// Riceve il valore di Fullness del player e regola il Rate over Time del VFX child.
    /// </summary>
    /// <param name="fullness">Il valore attuale di Fullness del player (tra -1 e 1).</param>
    public void UpdateEmissionRate(float fullness)
    {
        if (emissionChildVFX == null) return;

        _currentPlayerFullness = fullness;

        // 1. Normalizziamo il Fullness da [-1, 1] a [0, 1]
        // (es.: -1f -> 0f; 0f -> 0.5f; 1f -> 1f)
        float normalizedFullness = (fullness + 1f) / 2f;

        // 2. Mappiamo il valore normalizzato tra minRateOverTime e maxRateOverTime
        float targetRate = Mathf.Lerp(minRateOverTime, maxRateOverTime, normalizedFullness);

        // 3. Applichiamo il nuovo Rate over Time al modulo di emissione
        // Modifichiamo il valore 'constant' della curva
        _emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(targetRate);

        // 🔥 DEBUG: Stampa il valore di Fullness e il Rate over Time modificato
        //Debug.Log($"[{gameObject.name} Ricevente] Fullness Player: {fullness:F2} -> Rate Over Time: {targetRate:F2}");
    }

    /// <summary>
    /// Esegue la rotazione del VFX root verso il player.
    /// </summary>
    private void RotateVFXTowardsPlayer()
    {
        if (incomingVFX == null || _playerTransform == null) return;

        // Calcola la direzione dal VFX root (che è l'oggetto da ruotare) al Player
        Vector3 dir = _playerTransform.position - incomingVFX.transform.position;
        dir.y = 0f; // Manteniamo la rotazione solo sull'asse Y (planare)

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);

            // Rotazione fluida tramite interpolazione
            incomingVFX.transform.rotation = Quaternion.Lerp(
                incomingVFX.transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}