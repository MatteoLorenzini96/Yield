using UnityEngine;

[RequireComponent(typeof(NPCControllerProximity))]
public class NPCBehaviorByPlayerProximity : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FullnessController _playerFullness;
    [SerializeField] private NPCControllerProximity _npcController;
    [SerializeField] private SphereCollider _detectionTrigger; // trigger per rilevare il player

    private int _currentState = -1;

    private void Awake()
    {
        if (_npcController == null)
            _npcController = GetComponent<NPCControllerProximity>();

        if (_detectionTrigger == null)
        {
            Debug.LogError($"{name}: Devi assegnare un SphereCollider per il trigger!");
            enabled = false;
            return;
        }

        if (!_detectionTrigger.isTrigger)
            _detectionTrigger.isTrigger = true; // assicurati sia trigger

        if (_playerFullness == null)
        {
            _playerFullness = FindFirstObjectByType<FullnessController>();
            if (_playerFullness == null)
                Debug.LogError("Player FullnessController non trovato!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_playerFullness != null && other.transform == _playerFullness.transform)
        {
            EvaluatePlayerFullness();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (_playerFullness != null && other.transform == _playerFullness.transform)
        {
            EvaluatePlayerFullness();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_playerFullness != null && other.transform == _playerFullness.transform)
        {
            _currentState = 0;
            _npcController.StopMovement();
        }
    }

    private void EvaluatePlayerFullness()
    {
        if (_playerFullness == null) return;

        float percent = Mathf.InverseLerp(-1f, 1f, _playerFullness.CurrentFullness) * 100f;
        // Debug: percentuale di full del player
        Debug.Log($"[NPCBehavior] Player Fullness: {percent:F1}%");

        int newState = _currentState;

        if (percent >= 75f)
            newState = 1;   // Wander lentamente
        else if (percent >= 25f)
            newState = 2;   // Corre verso il player
        else if (percent >= 1f)
            newState = 3;   // Blocca il player
        else if (percent == -1f)
            newState = 4;   // RunAway

        // Controlla anche la fullness dell'NPC
        var npcFullness = _npcController.GetComponent<NPCFullnessController>();
        if (npcFullness != null && npcFullness.CurrentFullness >= 1f)
            newState = 4;   // RunAway se NPC full

        if (newState == _currentState) return;

        _currentState = newState;
        HandleBehavior(newState);
    }

    private void HandleBehavior(int state)
    {
        switch (state)
        {
            case 1:
                _npcController.WanderSlow();
                break;
            case 2:
                _npcController.ApproachPlayer();
                break;
            case 3:
                _npcController.BlockPlayer();
                break;
            case 4:
                _npcController.RunAway();
                break;
        }
    }
}
