using UnityEngine;

[RequireComponent(typeof(NPCController))]
public class NPCBehaviorByPlayerProximity : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FullnessController _playerFullness;
    [SerializeField] private NPCController _npcController;
    [SerializeField] private SphereCollider _detectionTrigger; // assegnato dall’Inspector

    private int _currentState = -1;
    private bool _playerInRange = false;

    private void Awake()
    {
        if (!_npcController)
            _npcController = GetComponent<NPCController>();

        if (_detectionTrigger == null)
        {
            Debug.LogError($"{name}: Devi assegnare un SphereCollider per il trigger!");
            enabled = false;
            return;
        }

        if (!_detectionTrigger.isTrigger)
            _detectionTrigger.isTrigger = true; // assicurati sia trigger
    }

    private void Start()
    {
        if (_playerFullness == null)
            Debug.LogError("PlayerFullnes non assegnato");
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
        if (_playerInRange)
        {
            EvaluatePlayerFullness();
        }
    }

    private void EvaluatePlayerFullness()
    {
        if (_playerFullness == null) return;

        float rawFullness = _playerFullness.CurrentFullness;

        // DEBUG: mostra valore reale e percentuale
        float percent = Mathf.InverseLerp(-1f, 1f, rawFullness) * 100f;
        Debug.Log($"[NPCBehavior] Player Fullness: {percent:F1}% (Raw: {rawFullness:F2})");

        int newState;

        if (rawFullness >= 1f)
            newState = 1; // cammina lentamente
        else if (rawFullness >= 0.25f)
            newState = 2; // corre verso il player
        else if (rawFullness > 0f)
            newState = 3; // blocca il player
        else if (rawFullness == 0f)
            newState = 4; // scappa
        else
            newState = _currentState; // valori negativi o fuori range → mantieni stato corrente

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
