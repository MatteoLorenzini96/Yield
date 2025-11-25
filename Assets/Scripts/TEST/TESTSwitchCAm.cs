using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    // Array per memorizzare le posizioni della telecamera (GameObjects)
    public Transform[] cameraPositions;

    // Variabile per la telecamera che vuoi muovere
    public Camera mainCamera;

    // Variabile privata per tenere traccia della posizione attuale
    private int _currentIndex = 0;

    void Start()
    {
        // Assicurati che ci siano posizioni e che la telecamera principale sia assegnata
        if (mainCamera == null)
        {
            Debug.LogError($"{name}: Telecamera principale non assegnata!");
            enabled = false;
            return;
        }

        if (cameraPositions.Length == 0)
        {
            Debug.LogError($"{name}: Array cameraPositions vuoto!");
            enabled = false;
            return;
        }

        // Imposta subito la telecamera sulla prima posizione all'avvio
        SwitchCameraPosition(_currentIndex);
    }

    // Update viene chiamato ogni frame
    void Update()
    {
        // ➡️ Scorrimento AVANTI (Tasto '2')
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Incrementa l'indice e usa l'operatore Modulo (%) per tornare a 0
            // se superiamo la lunghezza dell'array.
            _currentIndex = (_currentIndex + 1) % cameraPositions.Length;
            SwitchCameraPosition(_currentIndex);
        }

        // ⬅️ Scorrimento INDIETRO (Tasto '1')
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Decrementa l'indice. 
            // La formula (+ length) % length gestisce il ritorno all'ultima posizione (length - 1)
            // se l'indice corrente è 0.
            _currentIndex = (_currentIndex - 1 + cameraPositions.Length) % cameraPositions.Length;
            SwitchCameraPosition(_currentIndex);
        }
    }

    // Funzione per spostare la telecamera alla posizione specifica
    void SwitchCameraPosition(int index)
    {
        if (index >= 0 && index < cameraPositions.Length)
        {
            // Muove la telecamera alla posizione del GameObject indicato nell'array
            mainCamera.transform.position = cameraPositions[index].position;
            mainCamera.transform.rotation = cameraPositions[index].rotation;

            Debug.Log($"Passaggio alla telecamera in posizione: {index + 1}");
        }
        else
        {
            Debug.LogWarning("Indice di posizione fuori range! (Questo non dovrebbe succedere con il codice Update modificato)");
        }
    }
}