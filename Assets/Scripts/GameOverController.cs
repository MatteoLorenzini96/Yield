using UnityEngine;
using System.Collections;

public class GameOverController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Il FullnessController da monitorare.")]
    [SerializeField] private FullnessController _fullnessController;

    [Tooltip("Il Renderer del materiale da modificare (lo stesso usato in FullnessController).")]
    [SerializeField] private Renderer _targetRenderer;

    [Tooltip("Il Canvas/GameObject da attivare a Game Over.")]
    [SerializeField] private GameObject _gameOverCanvas;

    [Header("Material Lerp Settings")]
    [Tooltip("L'indice del materiale nell'array del Renderer da modificare (0 per MAT_Player_Glass).")]
    [SerializeField] private int _materialIndex = 0; // <--- Target: Element 0 (MAT_Player_Glass)

    [Tooltip("Il nome della proprietà colore/vettore da modificare (es. _FresnelColor).")]
    [SerializeField] private string _fresnelColorProperty = "_FresnelColor";

    [Tooltip("La durata in secondi della transizione del colore a 0.")]
    [SerializeField] private float _fadeDuration = 3f;

    [Tooltip("La soglia di fullness per attivare il Game Over (vicino a -1).")]
    [SerializeField] private float _gameOverThreshold = -0.99f;

    // ⭐ NUOVA VARIABILE ⭐
    [Header("Game Over Timing")]
    [Tooltip("Il tempo in secondi da attendere dopo il Game Over prima di iniziare il fade.")]
    [SerializeField] private float _preFadeDelay = 1.0f;

    [Header("State")]
    private Material _materialInstance;
    private bool _isGameOver = false;
    private int _fresnelColorID;
    private Color _initialFresnelColor;

    private void Awake()
    {
        if (_fullnessController == null || _targetRenderer == null || _gameOverCanvas == null)
        {
            Debug.LogError("GameOverController: mancano le dipendenze essenziali!");
            enabled = false;
            return;
        }

        // Verifica che l'indice del materiale sia valido
        if (_targetRenderer.materials.Length <= _materialIndex)
        {
            Debug.LogError($"GameOverController: L'indice materiale {_materialIndex} è fuori dai limiti dell'array materiali del Renderer!");
            enabled = false;
            return;
        }

        // Ottiene un'istanza del materiale all'indice specificato
        _materialInstance = _targetRenderer.materials[_materialIndex];

        // Cerca l'ID della proprietà per prestazioni
        _fresnelColorID = Shader.PropertyToID(_fresnelColorProperty);

        // Salva il colore iniziale per l'interpolazione
        if (_materialInstance.HasProperty(_fresnelColorID))
        {
            _initialFresnelColor = _materialInstance.GetColor(_fresnelColorID);
        }
        else
        {
            Debug.LogError($"GameOverController: La proprietà {_fresnelColorProperty} non esiste nello shader sul Materiale all'indice {_materialIndex}!");
            enabled = false;
            return;
        }

        _gameOverCanvas.SetActive(false);
    }

    private void Update()
    {
        // Controlla la condizione di Game Over (-1)
        if (!_isGameOver && _fullnessController.CurrentFullness <= _gameOverThreshold)
        {
            TriggerGameOver();
            // Assicurati che NPCManager.Instance non sia null prima di chiamare il metodo
            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.MakeAllRunAway();
            }
        }

        // ⭐ NUOVO CONTROLLO: Uscita con ESC ⭐
        if (_isGameOver && Input.GetKeyDown(KeyCode.Escape))
        {
            Quit();
        }
    }

    private void TriggerGameOver()
    {
        _isGameOver = true;
        _fullnessController.enabled = false;
        // ⭐ AVVIA LA NUOVA COROUTINE CON IL RITARDO ⭐
        StartCoroutine(GameOverSequence());
    }

    // ⭐ NUOVA COROUTINE: Gestisce il ritardo prima del fade ⭐
    private IEnumerator GameOverSequence()
    {
        // Attende il tempo pre-fade
        yield return new WaitForSeconds(_preFadeDelay);

        // Avvia la dissolvenza del materiale
        yield return StartCoroutine(FadeMaterialColorIntensity());

        // Attiva il Canvas del Game Over
        ActivateGameOverCanvas();
    }

    private IEnumerator FadeMaterialColorIntensity()
    {
        float elapsedTime = 0f;
        Color targetColor = Color.black;

        while (elapsedTime < _fadeDuration)
        {
            float t = elapsedTime / _fadeDuration;
            // Usa SmoothStep per un'interpolazione più fluida
            t = Mathf.SmoothStep(0f, 1f, t);

            // Interpolazione: dal colore iniziale al nero (zero intensità)
            Color lerpedColor = Color.Lerp(_initialFresnelColor, targetColor, t);

            // Applica la modifica
            _materialInstance.SetColor(_fresnelColorID, lerpedColor);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Assicura che il colore sia esattamente nero (zero intensità) alla fine
        _materialInstance.SetColor(_fresnelColorID, targetColor);
    }

    private void ActivateGameOverCanvas()
    {
        _gameOverCanvas.SetActive(true);
        MouseCursorManager.Instance.ShowCursor();
        //Time.timeScale = 0f; // Scommenta se vuoi fermare il gioco completamente
    }

    // ⭐ NUOVA FUNZIONE PUBBLICA per l'uscita ⭐
    /// <summary>
    /// Esce dall'applicazione (o dalla modalità Play nell'editor).
    /// </summary>
    public void Quit()
    {
        //Debug.Log("QUIT: Uscita dall'applicazione richiesta.");
#if UNITY_EDITOR
        // Interrompe la modalità Play nell'editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // Esce dall'applicazione standalone
            Application.Quit();
#endif
    }
}