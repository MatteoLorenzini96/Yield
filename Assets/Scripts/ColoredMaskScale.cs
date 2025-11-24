using UnityEngine;
using System.Collections;

public class ColoredMaskScale : MonoBehaviour
{
    [Header("Oggetto da gestire")]
    public GameObject targetObject;

    [Header("Impostazioni Scale")]
    public float lerpDuration = 1f; // Durata dell'animazione

    private Coroutine scaleCoroutine;

    /// <summary>
    /// Attiva l'oggetto assegnato.
    /// </summary>
    public void ActivateObject()
    {
        if (targetObject != null)
            targetObject.SetActive(true);
    }

    // -------------------------
    // FUNZIONI DI SCALA ASSOLUTE
    // -------------------------
    public void ScaleTo0()
    {
        StartScaleTo(Vector3.one * 0f);
    }

    public void ScaleTo1()
    {
        StartScaleTo(Vector3.one * 1f);
    }

    public void ScaleTo2()
    {
        StartScaleTo(Vector3.one * 2f);
    }

    public void ScaleTo3()
    {
        StartScaleTo(Vector3.one * 3f);
    }

    // -------------------------
    // LOGICA DI LERP
    // -------------------------

    private void StartScaleTo(Vector3 targetScale)
    {
        if (targetObject == null) return;

        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(ScaleLerpRoutine(targetScale));
    }

    private IEnumerator ScaleLerpRoutine(Vector3 targetScale)
    {
        Vector3 startScale = targetObject.transform.localScale;
        float elapsed = 0f;

        while (elapsed < lerpDuration)
        {
            float t = elapsed / lerpDuration;
            targetObject.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Assicura la scala finale corretta
        targetObject.transform.localScale = targetScale;
    }
}
