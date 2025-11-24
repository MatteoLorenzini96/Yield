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

    // ⬅️ SALVA lo stato di scala già attivato per NON ripetere i lerp
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

        // --- ✔️ FIX: Ottiene automaticamente lo script ColoredMaskScale se non assegnato
        if (!_maskScale)
        {
            _maskScale = GetComponent<ColoredMaskScale>();
            if (!_maskScale)
            {
                Debug.LogWarning($"{name}: Nessuno script ColoredMaskScale trovato nell'oggetto.");
            }
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
        //     LOGICA DI SCALING A EVENTO UNICO
        // ------------------------------------------

        int scaleLevel = -1;

        if (_fullness > -1f)
            scaleLevel = 0; // attiva
        if (_fullness >= 0f)
            scaleLevel = 1; // scala a 2
        if (_fullness >= 1f)
            scaleLevel = 2; // scala a 3

        // Se il livello NON è cambiato → NON rifare i lerp
        if (scaleLevel == _lastScaleLevel)
            return;

        _lastScaleLevel = scaleLevel;

        switch (scaleLevel)
        {
            case 0:
                _maskScale.ActivateObject();
                break;

            case 1:
                _maskScale.ScaleTo2();
                break;

            case 2:
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
