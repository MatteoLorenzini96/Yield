using UnityEngine;

/// <summary>
/// Singleton Manager per la gestione del cursore del mouse.
/// L'istanza persiste tra i cambi di scena (DontDestroyOnLoad).
/// </summary>
public class MouseCursorManager : MonoBehaviour
{
    // Convenzione C#: Le proprietà pubbliche statiche (Singleton) hanno la prima lettera MAIUSCOLA.
    public static MouseCursorManager Instance { get; private set; }

    private void Awake()
    {
        // 1. Implementazione del Singleton pulita e robusta.
        if (Instance == null)
        {
            Instance = this;
            // Garantisce la persistenza attraverso i caricamenti di scena.
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            // Se esiste già un'istanza, distruggi questa nuova per evitare duplicati.
            Debug.LogWarning($"[{nameof(MouseCursorManager)}] Tentativo di creare una seconda istanza. Distruzione del duplicato.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 2. Chiama la funzione di avvio richiesta.
        HideCursor();
    }

    /// <summary>
    /// Nasconde il cursore del mouse e lo blocca al centro dello schermo (modalità gioco FPS/TPS).
    /// </summary>
    public void HideCursor()
    {
        // Imposta la visibilità e lo stato di blocco.
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Uso di nameof() per messaggi di log più robusti e facili da refactorare.
        //Debug.Log($"[{nameof(MouseCursorManager)}] Cursore nascosto. Stato: {nameof(CursorLockMode.Locked)}.");
    }

    /// <summary>
    /// Mostra il cursore del mouse e lo sblocca (modalità UI/Menu).
    /// </summary>
    public void ShowCursor()
    {
        // Imposta la visibilità e lo stato di sblocco.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log($"[{nameof(MouseCursorManager)}] Cursore mostrato. Stato: {nameof(CursorLockMode.None)}.");
    }
}