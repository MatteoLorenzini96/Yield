using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform _player;

    [Header("Movement Settings")]
    [SerializeField] private float _stopDistance = 2f; // distanza minima dal player
    [SerializeField] private float _waitTime = 2f;     // secondi di attesa quando fermo

    [Header("RunAway Settings")]
    [SerializeField] private float _safeDistance = 6f;
    [SerializeField] private Collider _runAwayTrigger; // trigger assegnato dall’inspector

    private NavMeshAgent _agent;
    private Coroutine _activeRoutine;
    private bool _isRunningAway;
    private bool _isPersistentRunAway;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_runAwayTrigger != null)
            _runAwayTrigger.enabled = false;
    }

    /// <summary>
    /// Avvia il movimento verso il player.
    /// </summary>
    public void StartApproach()
    {
        if (!_player) return;
        RestartRoutine(ApproachRoutine());
    }

    /// <summary>
    /// Avvia la fuga.
    /// </summary>
    public void RunAway()
    {
        if (!_player) return;

        if (_runAwayTrigger != null)
            _runAwayTrigger.enabled = true;

        RestartRoutine(RunAwayRoutine());
    }

    /// <summary>
    /// Ferma il movimento e le routine.
    /// </summary>
    public void StopMovement()
    {
        if (_activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
            _activeRoutine = null;
        }

        _isRunningAway = false;
        _agent.isStopped = true;

        if (!_isPersistentRunAway && _runAwayTrigger != null)
            _runAwayTrigger.enabled = false;
    }

    /// <summary>
    /// Abilita/disabilita RunAway persistente.
    /// </summary>
    public void SetPersistentRunAway(bool value)
    {
        _isPersistentRunAway = value;
        if (_runAwayTrigger != null)
            _runAwayTrigger.enabled = value;

        if (_isPersistentRunAway && !_isRunningAway)
            RunAway();
    }

    //───────────────────────────────
    private IEnumerator ApproachRoutine()
    {
        while (true)
        {
            if (!_player) yield break;

            float distance = Vector3.Distance(transform.position, _player.position);

            if (distance <= _stopDistance)
            {
                _agent.isStopped = true;
                yield return new WaitForSeconds(_waitTime);
            }
            else
            {
                _agent.isStopped = false;
                _agent.SetDestination(_player.position);
            }

            yield return null;
        }
    }

    private IEnumerator RunAwayRoutine()
    {
        _isRunningAway = true;
        _agent.isStopped = false;

        while (_isRunningAway && _isPersistentRunAway && _player)
        {
            Vector3 fromPlayer = transform.position - _player.position;
            fromPlayer.y = 0f;

            if (fromPlayer.magnitude < _safeDistance)
            {
                Vector3 targetPos = transform.position + fromPlayer.normalized * _safeDistance;

                // Verifica percorso valido sulla NavMesh
                NavMeshPath path = new NavMeshPath();
                if (_agent.CalculatePath(targetPos, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    _agent.SetDestination(targetPos);
                    _agent.isStopped = false;
                }
                else
                {
                    // prova direzioni alternative ruotate di ±25° fino a ±75°
                    bool found = false;
                    for (int i = 1; i <= 3 && !found; i++)
                    {
                        float angle = 25f * i;
                        Vector3[] directions = {
                            Quaternion.Euler(0, angle, 0) * fromPlayer.normalized,
                            Quaternion.Euler(0, -angle, 0) * fromPlayer.normalized
                        };
                        foreach (var dir in directions)
                        {
                            Vector3 altPos = transform.position + dir * _safeDistance;
                            if (_agent.CalculatePath(altPos, path) && path.status == NavMeshPathStatus.PathComplete)
                            {
                                _agent.SetDestination(altPos);
                                _agent.isStopped = false;
                                found = true;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                _agent.isStopped = true; // fermati ma non uscire dalla routine
            }

            yield return null;
        }

        // Se RunAway non è più persistente, disattiva trigger e termina routine
        if (!_isPersistentRunAway && _runAwayTrigger != null)
            _runAwayTrigger.enabled = false;

        _isRunningAway = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_player || other.transform != _player) return;
        if (_runAwayTrigger != null && _runAwayTrigger.enabled)
            RestartRoutine(RunAwayRoutine()); // sempre pronto a scappare
    }

    private void RestartRoutine(IEnumerator routine)
    {
        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        _activeRoutine = StartCoroutine(routine);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _stopDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _safeDistance);
    }
#endif
}
