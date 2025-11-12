using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCControllerProximity : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform _player;

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

    private NavMeshAgent _agent;
    private Coroutine _activeRoutine;
    private bool _isRunningAway;

    private void Awake() => _agent = GetComponent<NavMeshAgent>();

    public void StopMovement()
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = null;
        _agent.isStopped = true;
    }

    public void WanderSlow() => RestartRoutine(WanderRoutine());
    public void ApproachPlayer() => RestartRoutine(ApproachRoutine());
    public void BlockPlayer() => RestartRoutine(BlockRoutine());
    public void RunAway()
    {
        if (_player == null) return;

        // RunAway SOLO se full del player == -1
        var playerFullness = _player.GetComponent<FullnessController>();
        if (playerFullness == null || playerFullness.CurrentFullness != -1f) return;

        RestartRoutine(RunAwayRoutine());
    }

    //───────────────────────────────
    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            Vector3 randomDir = Random.insideUnitSphere * wanderRadius + transform.position;
            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.speed = wanderSpeed;
                _agent.SetDestination(hit.position);
            }
            yield return new WaitForSeconds(wanderDelay);
        }
    }

    private IEnumerator ApproachRoutine()
    {
        _agent.speed = approachSpeed;
        while (true)
        {
            if (!_player) yield break;
            float dist = Vector3.Distance(transform.position, _player.position);
            _agent.isStopped = dist <= stopDistance;
            if (!_agent.isStopped) _agent.SetDestination(_player.position);
            yield return null;
        }
    }

    private IEnumerator BlockRoutine()
    {
        _agent.speed = blockSpeed;
        while (true)
        {
            if (!_player) yield break;
            float dist = Vector3.Distance(transform.position, _player.position);
            _agent.isStopped = dist <= stopDistance;
            if (!_agent.isStopped) _agent.SetDestination(_player.position);
            yield return null;
        }
    }

    private IEnumerator RunAwayRoutine()
    {
        _isRunningAway = true;
        _agent.speed = runAwaySpeed;
        while (_isRunningAway && _player)
        {
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
            else _agent.isStopped = true;

            yield return null;
        }
        _isRunningAway = false;
    }

    private void RestartRoutine(IEnumerator routine)
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(routine);
    }
}
