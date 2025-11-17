using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class MusicSetting
{
    public string clipName;
    [Range(0f, 1f)] public float volume = 1f;
}

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
    [SerializeField] private UnityEvent _onFullness49to25;
    [SerializeField] private UnityEvent _onFullnessBelow25;

    [Header("Music Settings")]
    [SerializeField] private MusicSetting _musicAbove75 = new MusicSetting { clipName = "Main1", volume = 1f };
    [SerializeField] private MusicSetting _musicAbove50 = new MusicSetting { clipName = "Main1", volume = 0.8f };
    [SerializeField] private MusicSetting _musicAbove25 = new MusicSetting { clipName = "Main2", volume = 1f };
    [SerializeField] private MusicSetting _musicBelow25 = new MusicSetting { clipName = "Main3", volume = 1f };

    [Header("Test Settings")]
    [SerializeField] private float _changeAmount = 0.1f;

    private Material _materialInstance;
    private float _lastFullness = Mathf.Infinity;
    private int _currentState = -1;

    public float CurrentFullness => _fullness;

    private void Awake()
    {
        if (_targetRenderer == null)
        {
            Debug.LogError("FullnessController: Nessun Renderer assegnato!");
            enabled = false;
            return;
        }

        _materialInstance = _targetRenderer.material;
        ApplyFullness();
    }

    private void Start()
    {
        // Garantisce che SoundManager sia pronto
        EvaluateFullnessState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
            SetFullness(_fullness - _changeAmount);

        if (Input.GetKeyDown(KeyCode.M))
            SetFullness(_fullness + _changeAmount);
    }

    public void SetFullness(float newValue)
    {
        newValue = Mathf.Clamp(newValue, -1f, 1f);

        if (Mathf.Approximately(newValue, _lastFullness))
            return;

        _fullness = newValue;
        _lastFullness = newValue;

        ApplyFullness();
        EvaluateFullnessState();
    }

    private void ApplyFullness()
    {
        if (_materialInstance != null)
            _materialInstance.SetFloat(_fullnessProperty, _fullness);
    }

    private void EvaluateFullnessState()
    {
        float percent = Mathf.InverseLerp(-1f, 1f, _fullness) * 100f;
        int newState = percent switch
        {
            >= 75f => 1,
            >= 50f => 2,
            >= 25f => 3,
            _ => 4
        };

        if (newState == _currentState)
            return;

        _currentState = newState;

        var sm = SoundManager.Instance;
        if (sm != null)
        {
            // Reset pitch di default
            sm.musicSourceA.pitch = 1f;
            sm.musicSourceB.pitch = 1f;
        }

        switch (newState)
        {
            case 1:
                _onFullness100to75?.Invoke();
                sm?.PlayMusic(_musicAbove75.clipName, _musicAbove75.volume);
                break;
            case 2:
                _onFullness74to50?.Invoke();
                if (sm != null)
                {
                    sm.PlayMusic(_musicAbove50.clipName, _musicAbove50.volume);
                    sm.musicSourceA.pitch = 0.8f;
                    sm.musicSourceB.pitch = 0.8f;
                }
                break;
            case 3:
                _onFullness49to25?.Invoke();
                sm?.PlayMusic(_musicAbove25.clipName, _musicAbove25.volume);
                break;
            case 4:
                _onFullnessBelow25?.Invoke();
                sm?.PlayMusic(_musicBelow25.clipName, _musicBelow25.volume);
                break;
        }
    }
}
