using UnityEngine;

[RequireComponent(typeof(NPCControllerProximity))]
public class NPCBehaviorByPlayerProximity : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FullnessController _playerFullness;
    [SerializeField] private NPCControllerProximity _npcController;
    [SerializeField] private SphereCollider _detectionTrigger;

    private int _currentState = -1;
    private bool _playerInRange = false;

    private void Awake()
    {
        if (!_npcController) _npcController = GetComponent<NPCControllerProximity>();
        if (_detectionTrigger == null)
        {
            Debug.LogError($"{name}: Devi assegnare un SphereCollider trigger!");
            enabled = false;
            return;
        }
        if (!_detectionTrigger.isTrigger) _detectionTrigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_playerFullness != null && other.transform == _playerFullness.transform)
        {
            _playerInRange = true;
            EvaluatePlayerFullness();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_playerFullness != null && other.transform == _playerFullness.transform)
        {
            _playerInRange = false;
            _currentState = 0;
            _npcController.StopMovement();
        }
    }

    private void Update()
    {
        if (_playerInRange) EvaluatePlayerFullness();
    }

    private void EvaluatePlayerFullness()
    {
        if (_playerFullness == null) return;

        float percent = Mathf.InverseLerp(-1f, 1f, _playerFullness.CurrentFullness) * 100f;
        Debug.Log($"[NPCBehavior] Player Fullness: {percent:F1}%");

        int newState;

        if (percent >= 75f && percent <= 100f)
        {
            newState = 1; // cammina lentamente intorno al punto di spawn
        }
        else if (percent >= 25f && percent <= 74f)
        {
            newState = 2; // corre verso il player
        }
        else if (percent >= 1f && percent <= 24f)
        {
            newState = 3; // blocca il player
        }
        else if (_playerFullness.CurrentFullness == -1f)
        {
            newState = 4; // scappa
        }
        else
        {
            // se non rientra in nessun range, resta nello stato precedente
            newState = _currentState;
        }

        if (newState == _currentState) return;
        _currentState = newState;
        HandleBehavior(newState);
    }

    private void HandleBehavior(int state)
    {
        switch (state)
        {
            case 1: _npcController.WanderSlow(); break;
            case 2: _npcController.ApproachPlayer(); break;
            case 3: _npcController.BlockPlayer(); break;
            case 4: _npcController.RunAway(); break;
        }
    }
}
