using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCControllerProximity : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform _player;
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
    [SerializeField] private float spawnWanderRadius = 2f;

    private Vector3 _spawnPosition;
    private NavMeshAgent _agent;
    private Coroutine _activeRoutine;
    private bool _isRunningAway;
    private bool _playerInRange = false;
    private bool _hasBeenInteracted = false;

    private NPCFullnessController _npcFullness;
    private FullnessController _playerFullness;

    // Animator
    private Animator animator;

    public event Action OnGiverDestroyed;

    public bool IsGiver => isGiver;
    public bool HasBeenInteracted => _hasBeenInteracted;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _npcFullness = GetComponent<NPCFullnessController>();
        _spawnPosition = transform.position;

        animator = GetComponentInChildren<Animator>();
        if (animator == null)
            Debug.LogError($"{name}: Nessun Animator trovato!");

        if (_detectionTrigger == null)
        {
            Debug.LogError($"{name}: assegna un SphereCollider per la rilevazione!");
            enabled = false;
            return;
        }

        if (!_detectionTrigger.isTrigger)
            _detectionTrigger.isTrigger = true;

        if (_player != null)
            _playerFullness = _player.GetComponent<FullnessController>();
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

    private void OnPlayerInteraction()
    {
        _hasBeenInteracted = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_player == null) return;
        if (other.transform == _player) _playerInRange = true;

        if (isGiver && other.transform == _player)
        {
            RestartRoutine(GiverRoutine());
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

    public void StopMovement()
    {
        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        _activeRoutine = null;
        _agent.isStopped = true;

        Debug.Log($"{name} ha fermato il movimento");
    }

    // ───────────── Metodi pubblici ─────────────
    public void WanderSlow() => RestartRoutine(WanderRoutine());
    public void ApproachPlayer() => RestartRoutine(ApproachRoutine());
    public void BlockPlayer() => RestartRoutine(BlockRoutine());
    public void RunAway() => RestartRoutine(RunAwayRoutine());

    // ───────────── Coroutine ─────────────
    private IEnumerator WanderRoutine()
    {
        while (_playerInRange || isGiver)
        {
            Vector3 randomDir = UnityEngine.Random.insideUnitSphere * wanderRadius + transform.position;

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.speed = wanderSpeed;
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

    private IEnumerator RunAwayRoutine()
    {
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
                _agent.isStopped = true;
                _isRunningAway = false;

                if (isGiver)
                {
                    Debug.Log($"{name} è stato distrutto dopo aver raggiunto la safe distance");
                    OnGiverDestroyed?.Invoke();
                    Destroy(gameObject);
                }
            }

            yield return null;
        }

        _agent.isStopped = true;
    }

    private IEnumerator GiverRoutine()
    {
        Debug.Log($"{name} ha iniziato GiverRoutine");

        while (!_playerInRange)
        {
            Vector3 randomDir = UnityEngine.Random.insideUnitSphere * spawnWanderRadius + _spawnPosition;

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, spawnWanderRadius, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.speed = wanderSpeed;
                _agent.SetDestination(hit.position);
            }

            yield return new WaitForSeconds(wanderDelay);
        }

        Debug.Log($"{name} si sta avvicinando al player");
        _agent.speed = approachSpeed;

        while (Vector3.Distance(transform.position, _player.position) > stopDistance)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);
            yield return null;
        }

        if (_playerFullness != null && _npcFullness != null)
        {
            float startPlayer = _playerFullness.CurrentFullness;
            float startNPC = _npcFullness.CurrentFullness;
            float duration = 3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _playerFullness.SetFullness(Mathf.Lerp(startPlayer, 1f, t));
                _npcFullness.SetFullness(Mathf.Lerp(startNPC, -1f, t));
                yield return null;
            }

            _playerFullness.SetFullness(1f);
            _npcFullness.SetFullness(-1f);

            Debug.Log($"{name} ha ricaricato il player e svuotato la propria fulleness");
        }

        Debug.Log($"{name} sta fuggendo");
        yield return RunAwayRoutine();
    }

    private void RestartRoutine(IEnumerator routine)
    {
        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        _activeRoutine = StartCoroutine(routine);
    }

    // ───────────── ANIMATOR UPDATE ─────────────
    private void UpdateAnimator()
    {
        if (animator == null || _agent == null)
            return;

        // Magnitude velocità orizzontale = input del Blend Tree
        Vector3 horizontalVel = new Vector3(_agent.velocity.x, 0f, _agent.velocity.z);
        float speed = horizontalVel.magnitude;

        animator.SetFloat("Speed", speed);
    }
}
