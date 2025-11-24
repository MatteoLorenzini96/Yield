using UnityEngine;
using System.Collections;

public class NPCFullnessController : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private Renderer _targetRenderer;

    [Header("Fullness Settings")]
    [Range(-1f, 1f)]
    [SerializeField] private float _fullness = 0f;

    [SerializeField] private float _transitionSpeed = 2f;

    [Header("Riferimento allo Scaling Controller")]
    [SerializeField] private ColoredMaskScale _maskScale;

    private static readonly int FullnessID = Shader.PropertyToID("_Fullness");
    private Material _materialInstance;
    private float _lastFullness;
    private Coroutine _transitionRoutine;

    // 🔹 Tiene traccia dell'ultimo livello di scala applicato
    private int _lastScaleLevel = -1;

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

        if (!_maskScale)
        {
            _maskScale = GetComponent<ColoredMaskScale>();
            if (!_maskScale)
                Debug.LogWarning($"{name}: Nessuno script ColoredMaskScale trovato nell'oggetto.");
        }

        _materialInstance = _targetRenderer.material;
        _lastFullness = _fullness;

        ApplyFullnessImmediate();
    }

    public void SetFullness(float newValue)
    {
        newValue = Mathf.Clamp(newValue, -1f, 1f);
        if (Mathf.Approximately(newValue, _lastFullness)) return;

        _lastFullness = newValue;

        if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(TransitionFullness(newValue));
    }

    public void AddFullness(float delta) => SetFullness(_lastFullness + delta);

    private IEnumerator TransitionFullness(float target)
    {
        float start = _fullness;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * _transitionSpeed;
            _fullness = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed));

            ApplyFullnessImmediate();
            yield return null;
        }

        _fullness = target;
        ApplyFullnessImmediate();
    }

    private void ApplyFullnessImmediate()
    {
        if (_materialInstance != null)
            _materialInstance.SetFloat(FullnessID, _fullness);

        if (_maskScale == null)
            return;

        // ------------------------------------------
        //           ⚡ NUOVA LOGICA DI SCALING
        // ------------------------------------------

        int scaleLevel = -1;

        // ⭐ LIVELLO 0 - SOLO quando FULLNESS = -1
        if (_fullness == -1f)
            scaleLevel = 0;

        // ⭐ LIVELLO 1 - quando FULLNESS > -1
        if (_fullness > -1f)
            scaleLevel = 1;

        // ⭐ LIVELLO 2 - FULLNESS >= -0.5
        if (_fullness >= -0.5f)
            scaleLevel = 2;

        // ⭐ LIVELLO 3 - FULLNESS >= 0
        if (_fullness >= 0f)
            scaleLevel = 3;

        // ⭐ LIVELLO 4 - FULLNESS >= 1
        if (_fullness >= 1f)
            scaleLevel = 4;

        // Evita ripetizioni (niente spam di Lerp)
        if (scaleLevel == _lastScaleLevel)
            return;

        _lastScaleLevel = scaleLevel;

        switch (scaleLevel)
        {
            case 0:
                _maskScale.ScaleTo0();     // SOLO per -1
                break;

            case 1:
                _maskScale.ActivateObject();
                break;

            case 2:
                _maskScale.ScaleTo1();
                break;

            case 3:
                _maskScale.ScaleTo2();
                break;

            case 4:
                _maskScale.ScaleTo3();
                break;
        }
    }

    private void OnDisable()
    {
        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);
    }
}
