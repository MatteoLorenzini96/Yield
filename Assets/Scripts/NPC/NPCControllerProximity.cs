using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
/// <summary>
/// Controlla il comportamento di un NPC (Wander, Approach, Block, RunAway)
/// basato sulla vicinanza al Player e gestisce la logica dei Giver.
/// </summary>
public class NPCControllerProximity : MonoBehaviour
{
    // === 1. FIELDS (Variabili serializzate) ===

    [Header("Target Settings")]
    [Tooltip("Il Transform del Player. Se non assegnato, cerca l'oggetto con il tag 'Player' in Awake.")]
    [SerializeField] private Transform _player;
    [Tooltip("SphereCollider utilizzato per rilevare il Player.")]
    [SerializeField] private SphereCollider _detectionTrigger;

    [Header("Movement Speeds")]
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float approachSpeed = 4f;
    [SerializeField] private float blockSpeed = 2f;
    [SerializeField] private float runAwaySpeed = 5f;

    [Header("Movement Settings")]
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float wanderDelay = 3f;

    [Header("RunAway Settings")]
    [SerializeField] private float safeDistance = 6f;

    [Header("Giver Settings")]
    [SerializeField] private bool isGiver = false;
    //[SerializeField] private float spawnWanderRadius = 2f;

    [Header("VFX & SFX Settings")]
    [SerializeField] private ParticleSystem fullChargeVFX;
    [SerializeField] private ParticleSystem giveEnergyVFX;
    [SerializeField] private GameObject fullVFXObject;
    [SerializeField] private string fullSFXName;

    // === 2. VARIABILI PRIVATE ===

    private Vector3 _spawnPosition;
    private NavMeshAgent _agent;
    private Coroutine _activeRoutine;
    private bool _isRunningAway;
    private bool _playerInRange = false;
    private bool _hasBeenInteracted = false;
    private bool hasPlayedFullVFX = false;

    private NPCFullnessController _npcFullness;
    private FullnessController _playerFullness;
    private ColoredMaskScale _playerColoredMaskScale;
    private WalkStepParticles_NavMeshAgent _walker;
    private Animator _animator;

    // === 3. EVENTI E PROPRIETÀ PUBBLICHE ===

    public event Action OnGiverDestroyed;
    public event Action OnFinishedRunAway;

    public bool IsGiver => isGiver;
    public bool HasBeenInteracted => _hasBeenInteracted;

    // === 4. UNITY LIFECYCLE METHODS ===

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _npcFullness = GetComponent<NPCFullnessController>();

        // Inizializzazione dei componenti opzionali
        _walker = GetComponent<WalkStepParticles_NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null) Debug.LogError($"{name}: Nessun Animator trovato!");
        if (_walker == null && !isGiver) Debug.LogWarning($"{name}: Nessun WalkStepParticles_NavMeshAgent trovato!");

        _spawnPosition = transform.position;

        // Assicurati che i VFX siano fermi all'inizio
        if (fullChargeVFX != null) fullChargeVFX.Stop();
        if (giveEnergyVFX != null) giveEnergyVFX.Stop();

        // Trigger check e setup
        if (_detectionTrigger == null)
        {
            Debug.LogError($"{name}: Assegna uno SphereCollider per la rilevazione!");
            enabled = false;
            return;
        }
        _detectionTrigger.isTrigger = true;

        // 🎯 LOGICA DI RICERCA DEL PLAYER TRAMITE TAG (Se _player è null)
        if (_player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");

            if (playerObject != null)
            {
                _player = playerObject.transform;
                //Debug.LogWarning($"{name}: Player assegnato automaticamente tramite Tag 'Player'.");
            }
        }

        // Inizializzazione dei componenti del Player
        if (_player != null)
        {
            _playerFullness = _player.GetComponent<FullnessController>();
            _playerColoredMaskScale = _player.GetComponent<ColoredMaskScale>();
        }
    }

    private void Start()
    {
        if (isGiver && _player != null)
        {
            // Abbonamento all'EnergyTransfer del Player se è un Giver
            var et = _player.GetComponent<EnergyTransfer>();
            if (et != null)
            {
                et.SubscribeToGiver(this);
            }
        }

        // 🎯 NUOVA LOGICA: Avvia il Wander all'inizio, se non è un Giver
        if (!isGiver)
        {
            WanderSlow();
        }

        UpdateAnimator();
    }

    private void OnEnable()
    {
        if (_player != null)
        {
            EnergyTransfer et = _player.GetComponent<EnergyTransfer>();
            if (et != null)
                et.OnLeftClick.AddListener(OnPlayerInteraction);
        }
    }

    private void OnDisable()
    {
        if (_player != null)
        {
            EnergyTransfer et = _player.GetComponent<EnergyTransfer>();
            if (et != null)
                et.OnLeftClick.RemoveListener(OnPlayerInteraction);
        }
    }

    private void Update()
    {
        UpdateAnimator();
    }

    // === 5. INPUT & TRIGGER HANDLERS ===

    private void OnPlayerInteraction()
    {
        _hasBeenInteracted = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_player == null) return;

        if (other.transform == _player)
        {
            _playerInRange = true;
            if (isGiver)
            {
                RestartRoutine(GiverRoutine());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_player == null) return;

        if (other.transform == _player)
        {
            _playerInRange = false;

            if (!isGiver)
                StopMovement();
        }
    }

    // === 6. COROUTINE MANAGEMENT ===

    public void StopMovement()
    {
        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        _activeRoutine = null;
        _agent.isStopped = true;
    }

    public void WanderSlow() => RestartRoutine(WanderRoutine());
    public void ApproachPlayer() => RestartRoutine(ApproachRoutine());
    public void BlockPlayer() => RestartRoutine(BlockRoutine());
    public void RunAway() => RestartRoutine(RunAwayRoutine());

    private void RestartRoutine(IEnumerator routine)
    {
        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        _activeRoutine = StartCoroutine(routine);
    }

    // === 7. COROUTINE LOGIC ===

    private IEnumerator WanderRoutine()
    {
        _agent.speed = wanderSpeed;

        // Condizione:
        // - Se è un Giver: continua sempre a vagare.
        // - Se NON è un Giver: continua a vagare finché il Player non è nel range (_playerInRange == false).
        while (isGiver || !_playerInRange)
        {
            // Calcola una posizione casuale nell'area di spawn (ora usa _spawnPosition come centro)
            Vector3 randomPoint = UnityEngine.Random.insideUnitSphere * wanderRadius;
            randomPoint += _spawnPosition; // Aggiusta per vagare attorno al punto di spawn

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.SetDestination(hit.position);
            }

            yield return new WaitForSeconds(wanderDelay);
        }

        _agent.isStopped = true;
    }

    private IEnumerator ApproachRoutine()
    {
        _agent.speed = approachSpeed;

        while (_playerInRange && _player != null)
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            _agent.isStopped = dist <= stopDistance;

            if (!_agent.isStopped)
                _agent.SetDestination(_player.position);

            yield return null;
        }

        _agent.isStopped = true;
    }

    private IEnumerator BlockRoutine()
    {
        _agent.speed = blockSpeed;

        while (_player != null)
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            _agent.isStopped = dist <= stopDistance;

            if (!_agent.isStopped)
                _agent.SetDestination(_player.position);

            yield return null;
        }

        _agent.isStopped = true;
    }

    private IEnumerator RunAwayRoutine()
    {
        if (!isGiver && !hasPlayedFullVFX && _npcFullness != null && _npcFullness.CurrentFullness >= 1f)
        {
            hasPlayedFullVFX = true;
            PlayFullVFX();
        }

        _isRunningAway = true;
        _agent.speed = runAwaySpeed;

        while (_isRunningAway)
        {
            if (_player == null) break;

            Vector3 fromPlayer = transform.position - _player.position;
            fromPlayer.y = 0f;

            if (fromPlayer.magnitude < safeDistance)
            {
                Vector3 targetPos = transform.position + fromPlayer.normalized * safeDistance;

                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, safeDistance, NavMesh.AllAreas))
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(hit.position);
                }
            }
            else
            {
                if (isGiver)
                {
                    // Logica di distruzione/completamento per Giver
                    _agent.isStopped = true;
                    _isRunningAway = false;
                    OnFinishedRunAway?.Invoke();
                    OnGiverDestroyed?.Invoke();
                    Destroy(gameObject);
                    break;
                }
                else
                {
                    // Non Giver: rimane in RunAway ma fermo, aspettando che il Player si avvicini di nuovo
                    _agent.isStopped = true;
                    OnFinishedRunAway?.Invoke();
                }
            }

            yield return null;
        }
    }

    private IEnumerator GiverRoutine()
    {
        // 1. Avvicinamento
        _agent.speed = approachSpeed;

        while (Vector3.Distance(transform.position, _player.position) > stopDistance)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);
            yield return null;
        }

        _agent.isStopped = true;

        // 2. Trasferimento Fullness
        if (_playerFullness != null && _npcFullness != null)
        {
            if (giveEnergyVFX != null) giveEnergyVFX.Play();

            float startPlayer = _playerFullness.CurrentFullness;
            float startNPC = _npcFullness.CurrentFullness;
            float duration = 3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Interpolazione simultanea di Player e NPC Fullness
                _playerFullness.SetFullness(Mathf.Lerp(startPlayer, 1f, t));
                _npcFullness.SetFullness(Mathf.Lerp(startNPC, -1f, t));
                yield return null;
            }

            if (giveEnergyVFX != null) giveEnergyVFX.Stop();

            _playerFullness.SetFullness(1f);

            // Attivazione dei bonus Player
            _playerColoredMaskScale?.ActivateObject();
            _playerColoredMaskScale?.ScaleTo3();
            _npcFullness.SetFullness(-1f);

            var speedBoost = _player.GetComponent<PlayerSpeedBoost>();
            if (speedBoost != null)
                speedBoost.ActivateBoost();
        }

        // 3. Fuga (attende la fine della RunAwayRoutine)
        yield return RunAwayRoutine();
    }

    // === 8. UTILITY METHODS ===

    private void UpdateAnimator()
    {
        if (_animator == null || _agent == null) return;

        // Calcola la velocità orizzontale
        Vector3 horizontalVel = new Vector3(_agent.velocity.x, 0f, _agent.velocity.z);
        float speed = horizontalVel.magnitude;
        _animator.SetFloat("Speed", speed);

        if (!isGiver && _npcFullness != null)
        {
            float fullness = _npcFullness.CurrentFullness;
            bool isAlmostEmpty = fullness < -0.52f;
            _animator.SetBool("IsAlmostEmpty", isAlmostEmpty);
        }
    }

    private void PlayFullVFX()
    {
        // VFX
        if (fullChargeVFX != null) fullChargeVFX.Play();
        if (fullVFXObject != null) fullVFXObject.SetActive(true);

        // SFX (Assumendo l'esistenza di SoundManager.Instance)
        // Nota: SoundManager.Instance deve essere gestito come Singleton in un altro script.
        if (!string.IsNullOrEmpty(fullSFXName) && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFXWithPitch(fullSFXName, transform);

        if (_walker != null)
            _walker._toPlay = true;
    }
}