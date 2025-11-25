using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ImageFader : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Image _targetImage;

    [Header("Fade Settings")]
    [SerializeField] private float _fadeDuration = 2f; // Assumo la durata di 2s dai tuoi log

    private void Awake()
    {
        if (_targetImage == null)
        {
            Debug.LogError("ImageFader: Nessun componente Image assegnato!");
            enabled = false;
            return;
        }

        //Debug.Log("ImageFader: Eseguito Awake. Imposto Alpha a 0.");
        // Imposta l'alpha a 0 in Awake per assicurare che non sia visibile prima del fade
        Color initialColor = _targetImage.color;
        initialColor.a = 0f;
        _targetImage.color = initialColor;
    }

    private void Start()
    {
        // Start() viene chiamato quando l'oggetto è attivato per la prima volta
        StartFadeIn();
    }

    public void StartFadeIn()
    {
        //Debug.Log("ImageFader: Avviato StartFadeIn().");
        StartCoroutine(FadeImageAlpha(_fadeDuration));
    }

    private IEnumerator FadeImageAlpha(float duration)
    {
        //Debug.Log($"FadeImageAlpha: Coroutine avviata. Durata: {duration}s");
        float elapsedTime = 0f;

        // Colore iniziale da cui partire (Alpha 0)
        Color startColor = _targetImage.color;
        startColor.a = 0f;
        _targetImage.color = startColor;

        while (elapsedTime < duration)
        {
            // 't' è la proporzione di tempo trascorso (da 0 a 1)
            float t = elapsedTime / duration;

            // LERP: Calcola l'alpha corrente da 0 a 1
            float currentAlpha = Mathf.Lerp(0f, 1f, t);

            // Applica il nuovo Alpha mantenendo le componenti RGB originali
            Color newColor = startColor;
            newColor.a = currentAlpha;

            _targetImage.color = newColor;

            //Debug.Log($"Alpha Corrente: {currentAlpha:F2} (t={t:F2})");

            // ⭐ FIX ESSENZIALE: Usa Time.unscaledDeltaTime per ignorare Time.timeScale = 0 ⭐
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // Assicura che l'alpha sia esattamente 1.0 (100%) alla fine
        Color finalColor = _targetImage.color;
        finalColor.a = 1f;
        _targetImage.color = finalColor;

        //Debug.Log("FadeImageAlpha: Coroutine completata. Alpha finale impostato a 1.0.");
    }
}