using UnityEngine;
using UnityEngine.Events;

public class FullnessController : MonoBehaviour
{
    [Header("Shader Settings")]
    [SerializeField] private Renderer _targetRenderer;
    [SerializeField] private string _fullnessProperty = "_Fullness";

    [Header("Value Settings")]
    [Range(-1f, 1f)]
    [SerializeField] private float _fullness = 0f;

    [Header("Fullness Events")]
    [SerializeField] private UnityEvent _onFullness100to75;
    [SerializeField] private UnityEvent _onFullness74to50;
    [SerializeField] private UnityEvent _onFullness49to20;
    [SerializeField] private UnityEvent _onFullnessBelow20;

    [Header("Test Settings")]
    [SerializeField] private float _changeAmount = 0.1f; // Quanto cambia premendo M o N

    private Material _materialInstance;
    private float _lastFullness = Mathf.Infinity;
    private int _currentState = -1;

    // Getter pubblico per leggere il valore corrente
    public float CurrentFullness => _fullness;

    private void Awake()
    {
        if (_targetRenderer == null)
        {
            Debug.LogError("FullnessController: Nessun Renderer assegnato!");
            enabled = false;
            return;
        }

        // Crea istanza materiale per non modificare l'originale
        _materialInstance = _targetRenderer.material;
        ApplyFullness();
    }

    private void Update()
    {
        // Test input: riduce il valore premendo N
        if (Input.GetKeyDown(KeyCode.N))
        {
            SetFullness(_fullness - _changeAmount);
        }

        // Test input: aumenta il valore premendo M
        if (Input.GetKeyDown(KeyCode.M))
        {
            SetFullness(_fullness + _changeAmount);
        }
    }

    /// <summary>
    /// Imposta il valore di _Fullness e aggiorna lo stato se cambia.
    /// </summary>
    public void SetFullness(float newValue)
    {
        newValue = Mathf.Clamp(newValue, -1f, 1f);

        // Se il valore non cambia, non facciamo nulla
        if (Mathf.Approximately(newValue, _lastFullness))
            return;

        _fullness = newValue;
        _lastFullness = newValue;

        ApplyFullness();
        EvaluateFullnessState();
    }

    /// <summary>
    /// Aggiorna il valore dello shader.
    /// </summary>
    private void ApplyFullness()
    {
        if (_materialInstance != null)
            _materialInstance.SetFloat(_fullnessProperty, _fullness);
    }

    /// <summary>
    /// Converte il valore da -1/1 a percentuale e chiama l'evento corretto.
    /// </summary>
    private void EvaluateFullnessState()
    {
        float percent = Mathf.InverseLerp(-1f, 1f, _fullness) * 100f;
        int newState = percent switch
        {
            >= 75f => 1,
            >= 50f => 2,
            >= 20f => 3,
            _ => 4
        };

        // Solo se cambia stato
        if (newState == _currentState)
            return;

        _currentState = newState;

        // Richiama evento corrispondente
        switch (newState)
        {
            case 1: _onFullness100to75?.Invoke(); break;
            case 2: _onFullness74to50?.Invoke(); break;
            case 3: _onFullness49to20?.Invoke(); break;
            case 4: _onFullnessBelow20?.Invoke(); break;
        }
    }
}
