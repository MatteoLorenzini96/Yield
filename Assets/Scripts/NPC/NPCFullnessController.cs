using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class NPCFullnessController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer _targetRenderer;
    [SerializeField] private NPCController _npcController;

    [Header("Fullness Settings")]
    [Range(-1f, 1f)]
    [SerializeField] private float _fullness = 0f;

    [Tooltip("Tempo necessario per passare da un valore di fullness all’altro")]
    [SerializeField] private float _transitionSpeed = 2f;

    private static readonly int FullnessID = Shader.PropertyToID("_Fullness");
    private Material _materialInstance;

    private float _lastFullness;
    private int _currentState = -1;
    private Coroutine _transitionRoutine;
    private bool _isInRunAwayState = false;

    private void Awake()
    {
        if (!_targetRenderer)
            _targetRenderer = GetComponent<Renderer>();

        if (!_targetRenderer)
        {
            Debug.LogError($"{name}: Nessun Renderer assegnato!");
            enabled = false;
            return;
        }

        _materialInstance = _targetRenderer.material;

        if (!_npcController)
            _npcController = GetComponent<NPCController>();

        _lastFullness = _fullness;
        ApplyFullnessImmediate();
    }

    private void Start()
    {
        EvaluateFullnessState();
    }

    private void Update()
    {
        // Debug input: mouse sinistro aumenta, destro diminuisce
        if (Input.GetMouseButtonDown(0))
            AddFullness(0.1f);
        else if (Input.GetMouseButtonDown(1))
            AddFullness(-0.1f);
    }

    public void SetFullness(float newValue)
    {
        newValue = Mathf.Clamp(newValue, -1f, 1f);
        if (Mathf.Approximately(newValue, _lastFullness))
            return;

        _lastFullness = newValue;

        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);

        _transitionRoutine = StartCoroutine(TransitionFullness(newValue));
    }

    public void AddFullness(float delta) => SetFullness(_lastFullness + delta);

    private IEnumerator TransitionFullness(float targetValue)
    {
        float startValue = _fullness;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * _transitionSpeed;
            _fullness = Mathf.Lerp(startValue, targetValue, Mathf.SmoothStep(0f, 1f, elapsed));
            ApplyFullnessImmediate();
            yield return null;
        }

        _fullness = targetValue;
        ApplyFullnessImmediate();
        EvaluateFullnessState();
    }

    private void ApplyFullnessImmediate()
    {
        if (_materialInstance != null)
            _materialInstance.SetFloat(FullnessID, _fullness);
    }

    private void EvaluateFullnessState()
    {
        float percent = Mathf.InverseLerp(-1f, 1f, _fullness) * 100f;
        int newState = percent switch
        {
            >= 100f => 1,
            >= 75f => 2,
            >= 20f => 3,
            _ => 4
        };

        bool wasInRunAway = _isInRunAwayState;
        _isInRunAwayState = newState == 1;

        if (_npcController != null)
        {
            _npcController.SetPersistentRunAway(_isInRunAwayState);

            if (_isInRunAwayState && !wasInRunAway)
                _npcController.RunAway();
        }

        if (newState == _currentState && !(newState == 1))
            return;

        _currentState = newState;
        HandleNPCBehavior(newState);
    }

    private void HandleNPCBehavior(int state)
    {
        if (_npcController == null) return;

        switch (state)
        {
            case 1:
                Debug.Log($"{name}: Stato 1 → RunAway persistente");
                break;

            case 2:
            case 3:
                Debug.Log($"{name}: Stato {state} → StopMovement");
                _npcController.StopMovement();
                break;
            case 4:
                Debug.Log($"{name}: Stato 4 → StartApproach");
                _npcController.StartApproach();
                break;
        }
    }

    public bool IsInRunAwayState() => _isInRunAwayState;

    private void OnDisable()
    {
        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);
    }
}
