using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(FullnessController))]
public class EnergyTransfer : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private SphereCollider detectionTrigger; // assegna dall’inspector
    [SerializeField] private string npcTag = "NPC";

    [Header("Transfer Settings")]
    [SerializeField] private float transferAmount = 0.1f;      // quanto il player può dare
    [SerializeField] private float npcMultiplier = 5f;         // quanto riceve l'NPC
    [SerializeField] private float playerReturnFraction = 0.05f; // quanto ritorna al player
    [SerializeField] private float transferDuration = 1f;      // tempo per il lerp

    private FullnessController _playerFullness;
    private List<NPCFullnessController> _npcsInRange = new List<NPCFullnessController>();
    private bool _inTransfer = false;

    private void Awake()
    {
        _playerFullness = GetComponent<FullnessController>();

        if (detectionTrigger == null)
        {
            Debug.LogError($"{name}: Devi assegnare un SphereCollider come detectionTrigger!");
            enabled = false;
            return;
        }

        if (!detectionTrigger.isTrigger)
            detectionTrigger.isTrigger = true;
    }

    private void Update()
    {
        if (_npcsInRange.Count == 0 || _inTransfer) return;

        if (Input.GetMouseButtonDown(0))
        {
            // Trova il NPC più vicino
            NPCFullnessController closestNPC = null;
            float minDist = Mathf.Infinity;

            foreach (var npc in _npcsInRange)
            {
                if (npc.CurrentFullness >= 1f) continue; // ignora NPC già full

                float dist = Vector3.Distance(transform.position, npc.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestNPC = npc;
                }
            }

            if (closestNPC != null && _playerFullness.CurrentFullness > -1f)
            {
                StartCoroutine(TransferEnergyRoutine(closestNPC));
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(npcTag))
        {
            var npc = other.GetComponent<NPCFullnessController>();
            if (npc != null && !_npcsInRange.Contains(npc))
                _npcsInRange.Add(npc);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(npcTag))
        {
            var npc = other.GetComponent<NPCFullnessController>();
            if (npc != null)
                _npcsInRange.Remove(npc);
        }
    }

    private IEnumerator TransferEnergyRoutine(NPCFullnessController npc)
    {
        _inTransfer = true;

        float playerStart = _playerFullness.CurrentFullness;
        float playerTarget = Mathf.Clamp(playerStart - transferAmount, -1f, 1f);

        float npcStart = npc.CurrentFullness;
        float npcTarget = Mathf.Clamp(npcStart + transferAmount * npcMultiplier, -1f, 1f);

        float elapsed = 0f;

        while (elapsed < transferDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transferDuration);

            _playerFullness.SetFullness(Mathf.Lerp(playerStart, playerTarget, t));
            npc.SetFullness(Mathf.Lerp(npcStart, npcTarget, t));

            yield return null;
        }

        // Piccola restituzione al player
        float returnAmount = transferAmount * npcMultiplier * playerReturnFraction;
        _playerFullness.SetFullness(Mathf.Clamp(_playerFullness.CurrentFullness + returnAmount, -1f, 1f));

        _inTransfer = false;
    }
}
