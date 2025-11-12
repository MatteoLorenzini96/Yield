using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NPCController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform _player;

    [Header("Movement Settings")]
    [SerializeField] private float _stopDistance = 2f;
    [SerializeField] private float _wanderRadius = 5f;
    [SerializeField] private float _wanderDelay = 3f;

    [Header("RunAway Settings")]
    [SerializeField] private float _safeDistance = 6f;

    private NavMeshAgent _agent;
    private Coroutine _activeRoutine;
    private bool _isRunningAway;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public void StopMovement()
    {
        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        _activeRoutine = null;
        _agent.isStopped = true;
    }

    public void WanderSlow()
    {
        RestartRoutine(WanderRoutine());
    }

    public void ApproachPlayer()
    {
        if (!_player) return;
        RestartRoutine(ApproachRoutine());
    }

    public void BlockPlayer()
    {
        if (!_player) return;
        RestartRoutine(BlockRoutine());
    }

    public void RunAway()
    {
        if (!_player) return;
        RestartRoutine(RunAwayRoutine());
    }

    //───────────────────────────────
    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            Vector3 randomDir = Random.insideUnitSphere * _wanderRadius;
            randomDir += transform.position;
            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, _wanderRadius, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.speed = 1.5f;
                _agent.SetDestination(hit.position);
            }

            yield return new WaitForSeconds(_wanderDelay);
        }
    }

    private IEnumerator ApproachRoutine()
    {
        _agent.speed = 4f;
        while (true)
        {
            if (!_player) yield break;

            float distance = Vector3.Distance(transform.position, _player.position);

            if (distance > _stopDistance)
            {
                _agent.isStopped = false;
                _agent.SetDestination(_player.position);
            }
            else
            {
                _agent.isStopped = true;
            }

            yield return null;
        }
    }

    private IEnumerator BlockRoutine()
    {
        _agent.speed = 2f;
        while (true)
        {
            if (!_player) yield break;

            float distance = Vector3.Distance(transform.position, _player.position);
            if (distance > _stopDistance)
            {
                _agent.isStopped = false;
                _agent.SetDestination(_player.position);
            }
            else
            {
                _agent.isStopped = true;
            }

            yield return null;
        }
    }

    private IEnumerator RunAwayRoutine()
    {
        _isRunningAway = true;
        _agent.speed = 5f;

        while (_isRunningAway && _player)
        {
            Vector3 fromPlayer = transform.position - _player.position;
            fromPlayer.y = 0f;

            if (fromPlayer.magnitude < _safeDistance)
            {
                Vector3 targetPos = transform.position + fromPlayer.normalized * _safeDistance;
                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, _safeDistance, NavMesh.AllAreas))
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(hit.position);
                }
            }
            else
            {
                _agent.isStopped = true;
            }

            yield return null;
        }
        _isRunningAway = false;
    }

    private void RestartRoutine(IEnumerator routine)
    {
        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        _activeRoutine = StartCoroutine(routine);
    }
}
