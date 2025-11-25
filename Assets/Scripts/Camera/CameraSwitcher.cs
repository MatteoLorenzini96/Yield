using UnityEngine;
using System.Collections;

/// <summary>
/// Gestisce il cambio di posizione della telecamera principale in modo fluido (Lerp)
/// Implementato come Singleton per un facile accesso da qualsiasi punto del codice.
/// </summary>
public class CameraSwitcher : MonoBehaviour
{
    // === 1. SINGLETON IMPLEMENTATION ===

    private static CameraSwitcher _instance;
    public static CameraSwitcher Instance
    {
        get
        {
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

    // === 2. FIELDS ===

    [Header("Configuration")]
    [Tooltip("Tutte le posizioni (Transform) tra cui la telecamera può switchare.")]
    [SerializeField] private Transform[] _cameraPositions;

    [Tooltip("Muri o zone da attivare/disattivare in base alla telecamera. La lunghezza dovrebbe essere uguale o minore a quella di Camera Positions.")]
    [SerializeField] private GameObject[] _invisibleWalls; // 🧱 NUOVO ARRAY

    [Tooltip("La telecamera principale che verrà mossa.")]
    [SerializeField] private Camera _mainCamera;

    [Header("Transition Settings")]
    [Tooltip("Durata in secondi della transizione tra le telecamere.")]
    [SerializeField] private float _transitionDuration = 0.5f;

    private int _currentIndex = 0;
    private Coroutine _transitionCoroutine;

    // === 3. UNITY LIFECYCLE METHODS ===

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        // ... (Controlli di validità)
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

        // Attiva i muri iniziali
        ToggleWalls(_currentIndex); // 🧱 CHIAMATA INIZIALE
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

    public void NextCamera()
    {
        _currentIndex = (_currentIndex + 1) % _cameraPositions.Length;
        StartTransition(_currentIndex);
    }

    public void PrevCamera()
    {
        _currentIndex = (_currentIndex - 1 + _cameraPositions.Length) % _cameraPositions.Length;
        StartTransition(_currentIndex);
    }

    // === 5. PRIVATE LOGIC METHODS ===

    private void StartTransition(int targetIndex)
    {
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }

        Transform targetTransform = _cameraPositions[targetIndex];

        // Avviamo la Coroutine di transizione
        _transitionCoroutine = StartCoroutine(SmoothMove(_mainCamera.transform, targetTransform.position, targetTransform.rotation));

        // 🧱 CHIAMATA AL TOGGLE
        ToggleWalls(targetIndex);

        //Debug.Log($"Inizio transizione verso la posizione: {targetIndex + 1}");
    }

    /// <summary>
    /// Attiva un muro invisibile specifico basato sull'indice della telecamera e disattiva gli altri.
    /// L'ultima posizione della telecamera disattiva tutti i muri.
    /// </summary>
    private void ToggleWalls(int wallIndex)
    {
        if (_invisibleWalls == null || _invisibleWalls.Length == 0) return;

        // Se l'indice corrisponde all'ULTIMA telecamera, disattiva tutti i muri.
        if (wallIndex == _cameraPositions.Length - 1)
        {
            //Debug.Log("Ultima telecamera selezionata: disattivo tutti i muri.");
            foreach (GameObject wall in _invisibleWalls)
            {
                if (wall != null) wall.SetActive(false);
            }
            return;
        }

        // Altrimenti, attiva il muro corrispondente e disattiva tutti gli altri.
        for (int i = 0; i < _invisibleWalls.Length; i++)
        {
            GameObject currentWall = _invisibleWalls[i];
            if (currentWall == null) continue;

            // Il muro deve essere attivo se il suo indice corrisponde all'indice della telecamera, 
            // e solo se l'indice non supera il numero di muri disponibili.
            bool shouldBeActive = (i == wallIndex && i < _invisibleWalls.Length - 1); // -1 perché l'ultimo indice delle camere è gestito sopra.

            if (currentWall.activeSelf != shouldBeActive)
            {
                currentWall.SetActive(shouldBeActive);
                // Debug.Log($"Muro [{i}] impostato su: {shouldBeActive}");
            }
        }
    }

    /// <summary>
    /// Coroutine che sposta e ruota il Transform di partenza al Transform di destinazione.
    /// </summary>
    private IEnumerator SmoothMove(Transform startTransform, Vector3 endPosition, Quaternion endRotation)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = startTransform.position;
        Quaternion startRotation = startTransform.rotation;

        while (elapsedTime < _transitionDuration)
        {
            float t = elapsedTime / _transitionDuration;

            // Interpolazione (Lerp) di posizione e rotazione
            startTransform.position = Vector3.Lerp(startPosition, endPosition, t);
            startTransform.rotation = Quaternion.Slerp(startRotation, endRotation, t);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        // Fissa la posizione e la rotazione finale
        startTransform.position = endPosition;
        startTransform.rotation = endRotation;

        _transitionCoroutine = null;
    }
}