using UnityEngine;
using System.Collections; // Necessario per le Coroutine

/// <summary>
/// Gestisce il cambio di posizione della telecamera principale in modo fluido (Lerp)
/// Implementato come Singleton per un facile accesso da qualsiasi punto del codice.
/// </summary>
public class CameraSwitcher : MonoBehaviour
{
    // === 1. SINGLETON IMPLEMENTATION (Convenzione: Proprietà statica pubblica) ===

    private static CameraSwitcher _instance;
    public static CameraSwitcher Instance
    {
        get
        {
            // Lazy initialization
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<CameraSwitcher>();
                if (_instance == null)
                {
                    Debug.LogError($"Errore: Non c'è nessun GameObject con il componente {nameof(CameraSwitcher)} in scena.");
                }
            }
            return _instance;
        }
    }

    // === 2. FIELDS (Convenzione: [SerializeField] per l'Inspector, `_` per privati) ===

    [Header("Configuration")]
    [Tooltip("Tutte le posizioni (Transform) tra cui la telecamera può switchare.")]
    [SerializeField] private Transform[] _cameraPositions;

    [Tooltip("La telecamera principale che verrà mossa.")]
    [SerializeField] private Camera _mainCamera;

    [Header("Transition Settings")]
    [Tooltip("Durata in secondi della transizione tra le telecamere.")]
    [SerializeField] private float _transitionDuration = 0.5f;

    private int _currentIndex = 0;
    private Coroutine _transitionCoroutine; // Riferimento alla Coroutine corrente

    // === 3. UNITY LIFECYCLE METHODS ===

    private void Awake()
    {
        // Convenzione Singleton: assicurarsi che ci sia solo una istanza
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        // Controlli di validità
        if (_mainCamera == null)
        {
            Debug.LogError($"{nameof(CameraSwitcher)}: Telecamera principale non assegnata!");
            enabled = false;
            return;
        }

        if (_cameraPositions == null || _cameraPositions.Length == 0)
        {
            Debug.LogError($"{nameof(CameraSwitcher)}: L'array delle posizioni è vuoto!");
            enabled = false;
            return;
        }

        // Imposta la telecamera sulla posizione iniziale immediatamente
        _mainCamera.transform.position = _cameraPositions[_currentIndex].position;
        _mainCamera.transform.rotation = _cameraPositions[_currentIndex].rotation;
    }

    private void Update()
    {
        // ➡️ Scorrimento AVANTI (Tasto '2')
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            NextCamera();
        }

        // ⬅️ Scorrimento INDIETRO (Tasto '1')
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PrevCamera();
        }
    }

    // === 4. PUBLIC API METHODS ===

    /// <summary>
    /// Passa alla posizione di telecamera successiva (gestisce il wrap-around).
    /// Accessibile tramite CameraSwitcher.Instance.NextCamera().
    /// </summary>
    public void NextCamera()
    {
        // Incrementa l'indice e usa l'operatore Modulo (%) per tornare a 0
        _currentIndex = (_currentIndex + 1) % _cameraPositions.Length;
        StartTransition(_currentIndex);
    }

    /// <summary>
    /// Passa alla posizione di telecamera precedente (gestisce il wrap-around).
    /// </summary>
    public void PrevCamera()
    {
        // Decrementa l'indice. Aggiunge Length per gestire correttamente i negativi con il modulo.
        _currentIndex = (_currentIndex - 1 + _cameraPositions.Length) % _cameraPositions.Length;
        StartTransition(_currentIndex);
    }

    // === 5. PRIVATE LOGIC METHODS ===

    private void StartTransition(int targetIndex)
    {
        // Fermiamo qualsiasi transizione in corso prima di iniziarne una nuova
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }

        Transform targetTransform = _cameraPositions[targetIndex];

        // Avviamo la Coroutine di transizione
        _transitionCoroutine = StartCoroutine(SmoothMove(_mainCamera.transform, targetTransform.position, targetTransform.rotation));

        //Debug.Log($"Inizio transizione verso la posizione: {targetIndex + 1}");
    }

    /// <summary>
    /// Coroutine che sposta e ruota il Transform di partenza al Transform di destinazione.
    /// Questa è la parte più ottimizzata: viene eseguita solo per la durata specificata.
    /// </summary>
    private IEnumerator SmoothMove(Transform startTransform, Vector3 endPosition, Quaternion endRotation)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = startTransform.position;
        Quaternion startRotation = startTransform.rotation;

        // Loop per la durata della transizione
        while (elapsedTime < _transitionDuration)
        {
            // Calcola la frazione (da 0.0 a 1.0) completata
            float t = elapsedTime / _transitionDuration;

            // Opzionale: Applicare Easing (smoother start/end)
            // float tSmooth = t * t * (3f - 2f * t); 

            // Interpolazione (Lerp) di posizione e rotazione
            startTransform.position = Vector3.Lerp(startPosition, endPosition, t);
            startTransform.rotation = Quaternion.Slerp(startRotation, endRotation, t);

            elapsedTime += Time.deltaTime;

            yield return null; // Attende il prossimo frame
        }

        // Assicuriamoci che la posizione e rotazione finale siano precise (a t=1.0)
        startTransform.position = endPosition;
        startTransform.rotation = endRotation;

        _transitionCoroutine = null; // Resetta il riferimento
        //Debug.Log("Transizione completata.");
    }
}