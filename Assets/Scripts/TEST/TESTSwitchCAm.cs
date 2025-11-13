using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    // Array per memorizzare le posizioni della telecamera (GameObjects)
    public Transform[] cameraPositions;

    // Variabile per la telecamera che vuoi muovere
    public Camera mainCamera;

    // Update viene chiamato ogni frame
    void Update()
    {
        // Verifica se il tasto 1 è stato premuto
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchCameraPosition(0); // Posizione 1
        }

        // Verifica se il tasto 2 è stato premuto
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchCameraPosition(1); // Posizione 2
        }

        // Verifica se il tasto 3 è stato premuto
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SwitchCameraPosition(2); // Posizione 3
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
        }
        else
        {
            Debug.LogWarning("Indice di posizione fuori range!");
        }
    }
}
