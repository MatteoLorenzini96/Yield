using UnityEngine;
using UnityEngine.SceneManagement; // Necessario per la gestione delle scene
using System;

/// <summary>
/// Gestisce la logica di passaggio alla prossima scena dopo la distruzione del Giver.
/// Inizia il processo di fade out sull'Animator assegnato.
/// </summary>
public class LevelChanger : MonoBehaviour
{
    // Variabile per l'Animator, assegnata nell'Inspector (deve essere l'Animator del Fade Canvas)
    [Header("Fade Settings")]
    [Tooltip("L'Animator sul Canvas che gestisce l'animazione di Fade In/Out.")]
    public Animator animator;

    // Variabile per il trigger dell'animatore
    private const string FadeOutTriggerName = "FadeOut";

    // Variabile per memorizzare il nome della scena da caricare DOPO il fade
    private string sceneToLoad;

    // --- Le tue variabili originali ---
    [Header("Game Logic Settings")]
    [Tooltip("L'oggetto da attivare quando il Giver viene distrutto (es. un'interfaccia o un testo).")]
    [SerializeField] private GameObject _objectToActivate;

    [Tooltip("Il nome della prossima scena da caricare.")]
    [SerializeField] private string _nextSceneName = "NomeDellaProssimaScena";

    [Header("Debug")]
    [Tooltip("Indica se l'evento OnGiverDestroyed è stato ricevuto.")]
    [SerializeField] private bool _giverDestroyed = false;

    private NPCControllerProximity _giverController;
    // ------------------------------------

    private void Awake()
    {
        if (_objectToActivate != null)
        {
            _objectToActivate.SetActive(false);
        }

        // Tenta di trovare il Giver nella scena.
        _giverController = FindFirstObjectByType<NPCControllerProximity>();

        if (_giverController != null)
        {
            _giverController.OnGiverDestroyed += OnGiverDestroyedHandler;
        }
        else
        {
            Debug.LogError("LevelChanger: NPCControllerProximity (Giver) non trovato nella scena.");
        }

        if (animator == null)
        {
            Debug.LogWarning("LevelChanger: Animator non assegnato. Il caricamento scena avverrà senza fade.");
        }
    }

    private void OnDestroy()
    {
        if (_giverController != null)
        {
            _giverController.OnGiverDestroyed -= OnGiverDestroyedHandler;
        }
    }

    private void Update()
    {
        // Verifica se l'evento è avvenuto e se l'utente ha premuto il tasto sinistro del mouse.
        if (_giverDestroyed && Input.GetMouseButtonDown(0))
        {
            FadeToNextScene(); // Avvia il fade
        }
    }

    private void OnGiverDestroyedHandler()
    {
        _giverDestroyed = true;

        // Attiva l'oggetto specificato
        if (_objectToActivate != null)
        {
            _objectToActivate.SetActive(true);
        }
    }

    /// <summary>
    /// Avvia l'animazione di fade out. La scena verrà caricata da FadeController al termine.
    /// </summary>
    private void FadeToNextScene()
    {
        if (string.IsNullOrEmpty(_nextSceneName) || _nextSceneName == "NomeDellaProssimaScena")
        {
            Debug.LogError("LevelChanger: Il nome della scena successiva non è stato impostato correttamente!");
            return;
        }

        sceneToLoad = _nextSceneName;

        if (animator != null)
        {
            // Attiva il trigger di FadeOut sull'Animator
            animator.SetTrigger(FadeOutTriggerName);
        }
        else
        {
            // Fallback se l'Animator non è assegnato
            FinishLevelChange();
        }

        // Disabilita ulteriori input mentre è in corso il caricamento
        enabled = false;
    }

    /// <summary>
    /// **Metodo PUBBLICO** chiamato dallo script FadeController al termine dell'animazione.
    /// Esegue il caricamento effettivo della scena.
    /// </summary>
    public void FinishLevelChange()
    {
        LoadSceneNow(sceneToLoad);
    }

    /// <summary>
    /// Carica effettivamente la scena, con gestione degli errori.
    /// </summary>
    private void LoadSceneNow(string sceneName)
    {
        try
        {
            SceneManager.LoadScene(sceneName);
        }
        catch (Exception e)
        {
            Debug.LogError($"LevelChanger: Errore durante il caricamento della scena '{sceneName}'. Assicurati che il nome sia corretto e sia stato aggiunto alle 'Scenes In Build'. Errore: {e.Message}");
        }
    }
}