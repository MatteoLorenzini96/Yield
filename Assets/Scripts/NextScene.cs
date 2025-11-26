using UnityEngine;
using UnityEngine.SceneManagement; // Necessario per la gestione delle scene
using System;

/// <summary>
/// Ascolta l'evento di distruzione del Giver e, al verificarsi, attiva un oggetto.
/// Permette di caricare la scena successiva con il click sinistro del mouse.
/// </summary>
public class NextScene : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("L'oggetto da attivare quando il Giver viene distrutto (es. un'interfaccia o un testo).")]
    [SerializeField] private GameObject _objectToActivate;

    [Tooltip("Il nome della prossima scena da caricare.")]
    [SerializeField] private string _nextSceneName = "NomeDellaProssimaScena";

    [Header("Debug")]
    [Tooltip("Indica se l'evento OnGiverDestroyed è stato ricevuto.")]
    [SerializeField] private bool _giverDestroyed = false;

    private NPCControllerProximity _giverController;

    private void Awake()
    {
        // Disattiva l'oggetto all'inizio, se è stato assegnato.
        if (_objectToActivate != null)
        {
            _objectToActivate.SetActive(false);
        }

        // Tenta di trovare il Giver nella scena.
        // ATTENZIONE: Questo funziona se c'è UN SOLO NPCControllerProximity attivo con isGiver=true.
        // Per scenari più complessi, è meglio che il Giver passi un riferimento.
        _giverController = FindFirstObjectByType<NPCControllerProximity>();

        if (_giverController != null)
        {
            // 💡 Abbonamento all'evento OnGiverDestroyed
            _giverController.OnGiverDestroyed += OnGiverDestroyedHandler;
            //Debug.Log($"NextScene: Abbonato all'evento OnGiverDestroyed di {_giverController.name}.");
        }
        else
        {
            Debug.LogError("NextScene: NPCControllerProximity (Giver) non trovato nella scena. L'ascolto dell'evento fallirà.");
        }
    }

    private void OnDestroy()
    {
        // 💡 Disabbonamento per prevenire memory leak, essenziale per gli eventi.
        if (_giverController != null)
        {
            _giverController.OnGiverDestroyed -= OnGiverDestroyedHandler;
            // Nel caso in cui questo oggetto venga distrutto prima del Giver.
        }
    }

    private void Update()
    {
        // Verifica se l'evento è avvenuto e se l'utente ha premuto il tasto sinistro del mouse.
        if (_giverDestroyed && Input.GetMouseButtonDown(0)) // 0 è il tasto sinistro del mouse
        {
            LoadNextScene();
        }
    }

    /// <summary>
    /// Metodo chiamato all'attivazione dell'evento OnGiverDestroyed.
    /// </summary>
    private void OnGiverDestroyedHandler()
    {
        _giverDestroyed = true;
        //Debug.Log("NextScene: Evento OnGiverDestroyed ricevuto. Attivazione oggetto.");

        // 1. Attiva l'oggetto specificato
        if (_objectToActivate != null)
        {
            _objectToActivate.SetActive(true);
        }
    }

    /// <summary>
    /// Carica la prossima scena.
    /// </summary>
    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(_nextSceneName) || _nextSceneName == "NomeDellaProssimaScena")
        {
            Debug.LogError("NextScene: Il nome della scena successiva non è stato impostato correttamente!");
            return;
        }

        try
        {
            SceneManager.LoadScene(_nextSceneName);
        }
        catch (Exception e)
        {
            Debug.LogError($"NextScene: Errore durante il caricamento della scena '{_nextSceneName}'. Assicurati che il nome sia corretto e sia stato aggiunto alle 'Scenes In Build'. Errore: {e.Message}");
        }
    }
}