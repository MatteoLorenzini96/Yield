using UnityEngine;
using UnityEngine.Events;
using System.Collections; // Necessario per IEnumerator

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

    // --- NUOVI CAMPI PER VFX ---
    [Header("VFX Settings")]
    [SerializeField] private GameObject _vfxOnFullnessReached; // Il GameObject del VFX
    [SerializeField] private float _vfxDuration = 3f; // Durata dopo l'attivazione

    [Header("Test Settings")]
    [SerializeField] private float _changeAmount = 0.1f;

    private Material _materialInstance;
    private float _lastFullness = Mathf.Infinity;
    private int _currentState = -1;

    // Flag per forzare la musica del case 1 la prima volta che entriamo nel case 3
    private bool _forceCase1UntilFull = false;
    private bool _firstCase3Encountered = false;

    // --- NUOVI FLAG DI STATO ---
    private bool _fullnessOneReachedOnce = false; // Traccia se la fullness ha raggiunto 1

    // --- NUOVI FLAG PER LA MUSICA (Stato di riproduzione singola) ---
    private bool _musicState1Played = false;
    private bool _musicState2Played = false;
    private bool _musicState3Played = false;
    private bool _musicState4Played = false;

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

        // Assicura che il VFX sia disattivato all'inizio
        if (_vfxOnFullnessReached != null)
        {
            _vfxOnFullnessReached.SetActive(false);
        }
    }

    private void Start()
    {
        // Imposta subito la musica corretta all'avvio
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

        // --- NUOVA LOGICA: CONTROLLA SE FULLNESS HA RAGGIUNTO 1 PER LA PRIMA VOLTA ---
        if (Mathf.Approximately(_fullness, 1f) && !_fullnessOneReachedOnce)
        {
            _fullnessOneReachedOnce = true;

            // **Aggiungi qui il Reset dei flag musica al raggiungimento di 1 la prima volta**
            ResetMusicPlayedFlags();

            ActivateVFX();
        }
    }

    private void ActivateVFX()
    {
        if (_vfxOnFullnessReached != null)
        {
            _vfxOnFullnessReached.SetActive(true);
            // Avvia la coroutine per disattivare il VFX dopo un certo tempo
            StartCoroutine(DeactivateVFXAfterDelay(_vfxDuration));
        }
    }

    private IEnumerator DeactivateVFXAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_vfxOnFullnessReached != null)
        {
            _vfxOnFullnessReached.SetActive(false);
        }
    }

    // --- NUOVA FUNZIONE PER RESETTARE I FLAG DI STATO MUSICA ---
    private void ResetMusicPlayedFlags()
    {
        _musicState1Played = false;
        _musicState2Played = false;
        _musicState3Played = false;
        _musicState4Played = false;
    }

    private void ApplyFullness()
    {
        if (_materialInstance != null)
            _materialInstance.SetFloat(_fullnessProperty, _fullness);
    }

    // --- NUOVA FUNZIONE PER RIPRODURRE LA MUSICA DELLO STATO ---
    private void PlayMusicForState(int state, SoundManager sm)
    {
        switch (state)
        {
            case 1:
                sm?.PlayMusic(_musicAbove75.clipName, _musicAbove75.volume);
                sm.musicSourceA.pitch = 1f;
                sm.musicSourceB.pitch = 1f;
                _musicState1Played = true;
                break;
            case 2:
                sm?.PlayMusic(_musicAbove50.clipName, _musicAbove50.volume);
                sm.musicSourceA.pitch = 0.8f;
                sm.musicSourceB.pitch = 0.8f;
                _musicState2Played = true;
                break;
            case 3:
                sm?.PlayMusic(_musicAbove25.clipName, _musicAbove25.volume);
                sm.musicSourceA.pitch = 1f;
                sm.musicSourceB.pitch = 1f;
                _musicState3Played = true;
                break;
            case 4:
                sm?.PlayMusic(_musicBelow25.clipName, _musicBelow25.volume);
                sm.musicSourceA.pitch = 1f;
                sm.musicSourceB.pitch = 1f;
                _musicState4Played = true;
                break;
        }
    }

    private void EvaluateFullnessState()
    {
        var sm = SoundManager.Instance;

        // Calcola lo stato corrente (1: >= 75%, 2: >= 50%, 3: >= 25%, 4: < 25%)
        float percent = Mathf.InverseLerp(-1f, 1f, _fullness) * 100f;
        int newState = percent switch
        {
            >= 75f => 1,
            >= 50f => 2,
            >= 25f => 3,
            _ => 4
        };

        // Gestione speciale: prima volta che si entra nel case 3 (forza musica 1)
        if (newState == 3 && !_firstCase3Encountered)
        {
            _firstCase3Encountered = true;
            _forceCase1UntilFull = true;
            _currentState = 1; // forza case 1
            _onFullness100to75?.Invoke();
            // La riproduzione musica qui DEVE essere eseguita per la logica di forzatura iniziale
            PlayMusicForState(1, sm);
            return;
        }

        // Se il flag è attivo, blocca solo se siamo ancora sotto 1
        if (_forceCase1UntilFull)
        {
            if (_fullness >= 1f)
            {
                _forceCase1UntilFull = false; // sblocca
            }
            else
            {
                if (_currentState != 1)
                {
                    _currentState = 1;
                    _onFullness100to75?.Invoke();
                    // La riproduzione musica qui DEVE essere eseguita per la logica di forzatura iniziale
                    PlayMusicForState(1, sm);
                }
                return;
            }
        }

        // --- NUOVA LOGICA DI RIPRODUZIONE MUSICA SINGOLA ---
        // Se la pienezza ha raggiunto 1 e lo stato non è cambiato, NON uscire ANCORA.
        // Devi eseguire gli eventi UnityEvent anche se la musica non cambia.
        if (_fullnessOneReachedOnce && newState == _currentState)
        {
            // Non fare nulla se siamo nello stesso stato DOPO aver raggiunto 1
            return;
        }

        // Se lo stato è lo stesso E NON siamo nella modalità di riproduzione singola (ovvero, prima che _fullness raggiunga 1)
        if (newState == _currentState && !_fullnessOneReachedOnce)
        {
            // La logica precedente bloccava tutto, manteniamo questo comportamento prima di aver raggiunto 1.
            return;
        }

        _currentState = newState;

        // Esegui gli eventi Unity e, se necessario, la musica.
        switch (newState)
        {
            case 1:
                _onFullness100to75?.Invoke();
                if (!_fullnessOneReachedOnce || !_musicState1Played)
                {
                    PlayMusicForState(1, sm);
                }
                break;
            case 2:
                _onFullness74to50?.Invoke();
                if (!_fullnessOneReachedOnce || !_musicState2Played)
                {
                    PlayMusicForState(2, sm);
                }
                break;
            case 3:
                _onFullness49to25?.Invoke();
                if (!_fullnessOneReachedOnce || !_musicState3Played)
                {
                    PlayMusicForState(3, sm);
                }
                break;
            case 4:
                _onFullnessBelow25?.Invoke();
                if (!_fullnessOneReachedOnce || !_musicState4Played)
                {
                    if (_firstCase3Encountered)
                    {
                        NPCManager.Instance.MakeAllBlockPlayer();
                        CameraSwitcher.Instance.NextCamera();
                    }
                    PlayMusicForState(4, sm);
                }
                else if (_fullnessOneReachedOnce && _musicState4Played)
                {
                    // L'unica eccezione dove la logica speciale DEVE essere eseguita sempre
                    // è lo stato 4 una volta raggiunto (per il blocco NPC e cambio telecamera)
                    if (_firstCase3Encountered)
                    {
                        NPCManager.Instance.MakeAllBlockPlayer();
                        CameraSwitcher.Instance.NextCamera();
                    }
                }
                break;
        }
    }
}