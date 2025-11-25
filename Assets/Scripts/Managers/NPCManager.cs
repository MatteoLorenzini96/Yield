using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;

    [SerializeField] private List<NPCControllerProximity> _activeNPCs = new();

    [Header("Configurazione Sincronizzazione Camera")]
    [Tooltip("La telecamera avanza quando il numero totale di NPC rimossi raggiunge questi valori (Decremento = Iniziale - Corrente).")]
    [SerializeField] public int[] CameraAdvanceThresholds = { 1, 3, 7 };
    [Tooltip("Ritardo (in secondi) usato per l'inizializzazione e per il controllo delle soglie.")]
    [SerializeField] private float _checkDelay = 1.0f;

    private int _initialNPCsCount = 0; // Inizializzato a 0, sarà impostato dopo il delay
    private int _thresholdsPassed = 0;
    private Coroutine _checkThresholdsCoroutine;

    public IReadOnlyList<NPCControllerProximity> ActiveNPCs => _activeNPCs;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ➡️ MODIFICA 1: Avvia la Coroutine per impostare il conteggio iniziale con un ritardo.
    private void Start()
    {
        StartCoroutine(InitializeWithDelay());
    }

    /// <summary>
    /// Coroutine che attende il delay e stabilisce il conteggio iniziale degli NPC.
    /// </summary>
    private IEnumerator InitializeWithDelay()
    {
        // ⏳ Fai aspettare il tempo specificato per permettere a tutti gli NPC di registrarsi.
        yield return new WaitForSeconds(_checkDelay);

        _initialNPCsCount = _activeNPCs.Count;

        // DEBUG RICHIESTO: Conteggio iniziale
        //Debug.Log($"[NPCManager - INIT] Conteggio iniziale stabilito (dopo {_checkDelay}s): {_initialNPCsCount}");

        if (_initialNPCsCount == 0)
        {
            Debug.LogWarning("[NPCManager - ATTENZIONE] Il conteggio iniziale degli NPC è 0. Le soglie non saranno mai raggiunte.");
        }
    }


    /// <summary>
    /// Viene chiamato dagli NPC quando si attivano
    /// </summary>
    public void RegisterNPC(NPCControllerProximity npc)
    {
        if (!_activeNPCs.Contains(npc))
        {
            _activeNPCs.Add(npc);
            // Non aggiorniamo qui _initialNPCsCount, ci fidiamo del conteggio iniziale ritardato.
        }
    }

    /// <summary>
    /// Viene chiamato dagli NPC quando devono essere rimossi (RunAway)
    /// </summary>
    public void UnregisterNPC(NPCControllerProximity npc)
    {
        if (_activeNPCs.Contains(npc))
        {
            _activeNPCs.Remove(npc);

            // Avvia o riavvia la Coroutine di controllo con ritardo
            if (_checkThresholdsCoroutine != null)
            {
                StopCoroutine(_checkThresholdsCoroutine);
            }
            _checkThresholdsCoroutine = StartCoroutine(CheckThresholdsWithDelay());
        }
    }

    // ---------------------------------------------------
    // LOGICA DI SOGLIA CON DEBUG
    // ---------------------------------------------------

    private IEnumerator CheckThresholdsWithDelay()
    {
        // ⏳ Aspetta il tempo specificato per stabilizzare il conteggio
        yield return new WaitForSeconds(_checkDelay);

        CheckNPCThresholds();

        _checkThresholdsCoroutine = null;
    }

    private void CheckNPCThresholds()
    {
        int currentCount = _activeNPCs.Count;

        // ➡️ MODIFICA 2: Conferma della logica del decremento
        // Il decremento è il numero di NPC rimossi rispetto al totale iniziale.
        int decrement = _initialNPCsCount - currentCount;

        // **DEBUG RICHIESTO**: Stato attuale del conteggio
        //Debug.Log($"\n--- [NPCManager - CHECK] Controllo Soglie Iniziato ---");
        //Debug.Log($"1. NPC Iniziali (Totale): {_initialNPCsCount}");
        //Debug.Log($"2. NPC Attivi (Corrente): {currentCount}");
        //Debug.Log($"3. Decremento Raggiunto: {decrement}");


        if (CameraSwitcher.Instance == null)
        {
            Debug.LogError("[NPCManager - ERRORE] CameraSwitcher.Instance è null. Impossibile avanzare la camera!");
            return;
        }

        if (_thresholdsPassed >= CameraAdvanceThresholds.Length)
        {
            //Debug.Log("[NPCManager - INFO] Tutte le soglie sono già state superate.");
            return;
        }

        // Recupera la prossima soglia da controllare
        int nextThreshold = CameraAdvanceThresholds[_thresholdsPassed];
        //Debug.Log($"4. Prossima Soglia Attesa (Indice {_thresholdsPassed}): {nextThreshold} NPC rimossi.");


        // Se il decremento ha raggiunto o superato la prossima soglia richiesta
        if (decrement >= nextThreshold)
        {
            // **DEBUG RICHIESTO**: Soglia superata e chiamata al NextCamera
            //Debug.Log($"\n✅ SOGLIA SUPERATA! {decrement} >= {nextThreshold}. CHIAMO CameraSwitcher.NextCamera().");

            CameraSwitcher.Instance.NextCamera();

            // Avanza al prossimo indice di soglia
            _thresholdsPassed++;
        }
        else
        {
            //Debug.Log($"❌ Soglia non raggiunta. Nessuna azione sulla camera.");
        }
        //Debug.Log($"--- [NPCManager - CHECK] Controllo Soglie Terminato ---\n");
    }

    // ---------------------------------------------------
    // CHIAMATE GLOBALI (Invariate)
    // ---------------------------------------------------

    public void MakeAllWanderSlow()
    {
        foreach (var npc in _activeNPCs)
            npc.WanderSlow();
    }

    public void MakeAllApproachPlayer()
    {
        foreach (var npc in _activeNPCs)
            npc.ApproachPlayer();
    }

    public void MakeAllBlockPlayer()
    {
        foreach (var npc in _activeNPCs)
            npc.BlockPlayer();
    }

    public void MakeAllRunAway()
    {
        foreach (var npc in new List<NPCControllerProximity>(_activeNPCs))
            npc.RunAway();
    }
}