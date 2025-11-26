using UnityEngine;

/// <summary>
/// Controllore allegato all'oggetto con l'Animator del Fade.
/// Ascolta l'Animation Event e, al completamento, notifica LevelChanger.
/// </summary>
public class FadeController : MonoBehaviour
{
    [Tooltip("Trascina qui l'oggetto che contiene lo script LevelChanger (es. GameManager).")]
    [SerializeField] private LevelChanger levelChangerScript;

    private void Start()
    {
        if (levelChangerScript == null)
        {
            Debug.LogError("FadeController: Riferimento a LevelChanger mancante. Impossibile gestire il caricamento della scena.");
        }
    }

    /// <summary>
    /// Metodo chiamato come **Animation Event** al termine della clip 'Fade_Out'.
    /// DEVE essere public e DEVE chiamarsi esattamente come impostato nell'Animation Event.
    /// </summary>
    public void OnFadeComplete()
    {
        if (levelChangerScript != null)
        {
            // Chiama il metodo di caricamento della scena sullo script LevelChanger
            levelChangerScript.FinishLevelChange();
        }
        else
        {
            Debug.LogError("FadeController: LevelChanger non assegnato o distrutto. Impossibile completare la transizione.");
        }
    }
}