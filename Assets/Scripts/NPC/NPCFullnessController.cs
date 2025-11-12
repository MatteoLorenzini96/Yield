using UnityEngine;
using System.Collections;

public class NPCFullnessController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer _targetRenderer;

    [Header("Fullness Settings")]
    [Range(-1f, 1f)]
    [SerializeField] private float _fullness = 0f;

    [Tooltip("Tempo necessario per passare da un valore di fullness all’altro")]
    [SerializeField] private float _transitionSpeed = 2f;

    private static readonly int FullnessID = Shader.PropertyToID("_Fullness");
    private Material _materialInstance;
    private Coroutine _transitionRoutine;

    public float CurrentFullness => _fullness;

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
        ApplyFullnessImmediate();
    }

    public void SetFullness(float newValue)
    {
        newValue = Mathf.Clamp(newValue, -1f, 1f);

        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);

        _transitionRoutine = StartCoroutine(TransitionFullness(newValue));
    }

    public void AddFullness(float delta) => SetFullness(_fullness + delta);

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
    }

    private void ApplyFullnessImmediate()
    {
        if (_materialInstance != null)
            _materialInstance.SetFloat(FullnessID, _fullness);
    }

    private void OnDisable()
    {
        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);
    }
}
