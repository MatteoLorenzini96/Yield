using UnityEngine;

[RequireComponent(typeof(NPCControllerProximity))]
public class NPCBehaviorByPlayerProximity : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FullnessController _playerFullness;
    [SerializeField] private NPCControllerProximity _npcController;
    [SerializeField] private SphereCollider _detectionTrigger;

    private NPCFullnessController _npcFullness;
    private int _currentState = -1;

    private void Awake()
    {
        if (_npcController == null)
            _npcController = GetComponent<NPCControllerProximity>();

        _npcFullness = GetComponent<NPCFullnessController>();

        if (_detectionTrigger == null)
        {
            Debug.LogError($"{name}: Devi assegnare un SphereCollider per il trigger!");
            enabled = false;
            return;
        }

        if (!_detectionTrigger.isTrigger)
            _detectionTrigger.isTrigger = true;

        if (_playerFullness == null)
        {
            _playerFullness = FindFirstObjectByType<FullnessController>();
            if (_playerFullness == null)
                Debug.LogError("Player FullnessController non trovato!");
        }

        // 🔹 REGISTRA NPC NEL MANAGER
        NPCManager.Instance?.RegisterNPC(_npcController);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_playerFullness != null && other.transform == _playerFullness.transform)
        {
            EvaluateBehavior();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (_playerFullness != null && other.transform == _playerFullness.transform)
        {
            EvaluateBehavior();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_playerFullness != null && other.transform == _playerFullness.transform)
        {
            _currentState = 0;

            if (!_npcController.IsGiver)
            {
                _npcController.StopMovement();
            }
        }
    }

    private void EvaluateBehavior()
    {
        if (_npcController.IsGiver)
        {
            NPCManager.Instance?.UnregisterNPC(_npcController);
            return;
        }
            
        if (_playerFullness == null || _npcFullness == null) return;

        // 🔹 SE L'NPC È FULL → SCAPPA
        if (_npcFullness.CurrentFullness >= 1f)
        {
            if (_currentState != 4)
            {
                NPCManager.Instance?.UnregisterNPC(_npcController);
                _npcController.RunAway();
                Debug.Log($"[{name}] NPC è pieno → scappa!");
            }

            _currentState = 4;
            return;
        }

        // 🔹 Valuta la fullness del player
        float percent = Mathf.InverseLerp(-1f, 1f, _playerFullness.CurrentFullness) * 100f;

        int newState = _currentState;

        if (percent >= 75f)
            newState = 1;
        else if (percent >= 25f)
            newState = 2;
        else if (percent >= 1f)
            newState = 3;
        else if (percent == -1f)
            newState = 4;

        if (newState == _currentState) return;

        _currentState = newState;
        HandleBehavior(newState);
    }

    private void HandleBehavior(int state)
    {
        switch (state)
        {
            case 1:
                Debug.Log($"{name} sta facendo WanderSlow");
                _npcController.WanderSlow();
                break;

            case 2:
                Debug.Log($"{name} sta facendo ApproachPlayer");
                _npcController.ApproachPlayer();
                break;

            case 3:
                Debug.Log($"{name} sta facendo BlockPlayer");
                _npcController.BlockPlayer();
                break;

            case 4:
                Debug.Log($"{name} sta facendo RunAway");
                NPCManager.Instance?.UnregisterNPC(_npcController);

                _npcController.RunAway();

                break;
        }
    }
}
