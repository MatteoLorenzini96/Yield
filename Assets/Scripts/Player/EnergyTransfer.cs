using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(FullnessController))]
public class EnergyTransfer : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private SphereCollider detectionTrigger;
    [SerializeField] private string npcTag = "NPC";

    [Header("Transfer Settings")]
    [SerializeField] private float transferAmount = 0.1f;
    [SerializeField] private float npcMultiplier = 5f;
    [SerializeField] private float playerReturnFraction = 0.05f;
    [SerializeField] private float transferDuration = 1f;

    [Header("Input Event")]
    public UnityEvent OnLeftClick = new UnityEvent();

    private FullnessController _playerFullness;
    private readonly List<NPCFullnessController> _npcsInRange = new List<NPCFullnessController>();
    private NPCFullnessController _closestNPC;
    private bool _inTransfer = false;
    private bool _giverDestroyed = false;

    private void Awake()
    {
        _playerFullness = GetComponent<FullnessController>();

        if (detectionTrigger == null)
        {
            Debug.LogError($"{name}: Devi assegnare un SphereCollider come detectionTrigger!");
            enabled = false;
            return;
        }
        detectionTrigger.isTrigger = true;
    }

    private void OnEnable()
    {
        OnLeftClick.AddListener(HandleTransferRequest);
    }

    private void OnDisable()
    {
        OnLeftClick.RemoveListener(HandleTransferRequest);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            OnLeftClick?.Invoke();
    }

    public void SubscribeToGiver(NPCControllerProximity giver)
    {
        giver.OnGiverDestroyed += HandleGiverDestroyed;
    }

    private void HandleGiverDestroyed()
    {
        _giverDestroyed = true;
        Debug.Log("Giver distrutto: EnergyTransfer ora abilitato");
    }

    private void HandleTransferRequest()
    {
        if (!_giverDestroyed) return; // Ignora finché il Giver non è distrutto
        if (_inTransfer || _closestNPC == null) return;

        float maxTransferable = _playerFullness.CurrentFullness - (-1f);
        if (maxTransferable <= 0f) return;

        float actualTransfer = Mathf.Min(transferAmount, maxTransferable);
        StartCoroutine(TransferEnergyRoutine(_closestNPC, actualTransfer));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(npcTag)) return;

        var npc = other.GetComponent<NPCFullnessController>();
        if (npc == null || _npcsInRange.Contains(npc)) return;

        _npcsInRange.Add(npc);
        UpdateClosestNPC();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(npcTag)) return;

        var npc = other.GetComponent<NPCFullnessController>();
        if (npc == null) return;

        _npcsInRange.Remove(npc);
        UpdateClosestNPC();
    }

    private void UpdateClosestNPC()
    {
        float minDist = Mathf.Infinity;
        _closestNPC = null;

        foreach (var npc in _npcsInRange)
        {
            if (npc == null || npc.CurrentFullness >= 1f) continue;

            float dist = Vector3.Distance(transform.position, npc.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                _closestNPC = npc;
            }
        }
    }

    private IEnumerator TransferEnergyRoutine(NPCFullnessController npc, float actualTransfer)
    {
        _inTransfer = true;

        float playerStart = _playerFullness.CurrentFullness;
        float playerTarget = Mathf.Clamp(playerStart - actualTransfer, -1f, 1f);

        float npcStart = npc.CurrentFullness;
        float npcTarget = Mathf.Clamp(npcStart + actualTransfer * npcMultiplier, -1f, 1f);

        float elapsed = 0f;

        while (elapsed < transferDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transferDuration);

            _playerFullness.SetFullness(Mathf.Lerp(playerStart, playerTarget, t));
            npc.SetFullness(Mathf.Lerp(npcStart, npcTarget, t));

            yield return null;
        }

        float returnAmount = actualTransfer * npcMultiplier * playerReturnFraction;
        _playerFullness.SetFullness(Mathf.Clamp(_playerFullness.CurrentFullness + returnAmount, -1f, 1f));

        _inTransfer = false;
        UpdateClosestNPC();
    }
}
