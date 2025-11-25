using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;

    [SerializeField] private List<NPCControllerProximity> _activeNPCs = new();

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

    /// <summary>
    /// Viene chiamato dagli NPC quando si attivano
    /// </summary>
    // NPCManager.cs
    public void RegisterNPC(NPCControllerProximity npc)
    {
        //Debug.Log($"Tentativo di registrazione di: {npc.name}. Manager Attivo: {Instance != null}");
        if (!_activeNPCs.Contains(npc))
        {
            _activeNPCs.Add(npc);
            //Debug.Log($"REGISTRAZIONE RIUSCITA. Totale NPC attivi: {_activeNPCs.Count}");
        }
        else
        {
            //Debug.Log($"ATTENZIONE: {npc.name} era già registrato.");
        }
    }

    /// <summary>
    /// Viene chiamato dagli NPC quando devono essere rimossi (RunAway)
    /// </summary>
    public void UnregisterNPC(NPCControllerProximity npc)
    {
        if (_activeNPCs.Contains(npc))
            _activeNPCs.Remove(npc);
    }

    // ---------------------------------------------------
    // CHIAMATE GLOBALI
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
            //Debug.Log("Attiva Tutti i player che bloccano");
    }

    public void MakeAllRunAway()
    {
        // Nota: questi NPC verranno rimossi quando ciascuno esegue RunAway()
        foreach (var npc in new List<NPCControllerProximity>(_activeNPCs))
            npc.RunAway();
    }
}
