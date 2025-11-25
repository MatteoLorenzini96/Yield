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

    [Header("Input Event")]
    public UnityEvent OnLeftClick = new UnityEvent();

    [Header("VFX Settings")]
    [SerializeField] private ParticleSystem transferVFX;
    [SerializeField] private float rotationSpeed = 10f;

    private FullnessController _playerFullness;
    private readonly List<NPCFullnessController> _npcsInRange = new List<NPCFullnessController>();
    private NPCFullnessController _closestNPC;

    private Transform _currentTransferTarget;
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

        if (transferVFX != null)
            transferVFX.Stop(); // assicura che sia fermo all'inizio
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
        // 🔥 Rotazione continua durante il transfer del VFX del Player verso l'NPC
        if (_inTransfer && _currentTransferTarget != null)
            RotateVFXTowardsNPC(_currentTransferTarget);

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
    }

    private void HandleTransferRequest()
    {
        if (!_giverDestroyed) return;
        if (_inTransfer || _closestNPC == null) return;

        if (_closestNPC.CurrentFullness >= 1f)
            return;

        // Assumi che EnergyTransferManager esista e sia accessibile
        float transferAmount = EnergyTransferManager.Instance.transferAmount;

        // 1. Calcola quanta Fullness il player può *effettivamente* dare
        // La Fullness minima è -1f, quindi il massimo trasferibile è la Fullness attuale meno il limite minimo.
        // Poiché il limite minimo è -1f, maxTransferable = CurrentFullness - (-1f) = CurrentFullness + 1f.
        // Ad esempio, se CurrentFullness è 0.5f, può trasferire fino a 1.5f.
        // Se CurrentFullness è -0.5f, può trasferire fino a 0.5f.
        float maxTransferable = _playerFullness.CurrentFullness - (-1f);

        // 2. LOGICA MODIFICATA: L'unico controllo per bloccare è se non si può trasferire *nulla* (maxTransferable <= 0f)
        // Se maxTransferable è > 0f, si può ancora trasferire, anche se è inferiore a transferAmount.
        if (maxTransferable <= 0f) return;

        // 3. Calcola il trasferimento effettivo:
        // È il minimo tra l'ammontare desiderato (transferAmount) e l'ammontare massimo disponibile (maxTransferable).
        // Se transferAmount > maxTransferable, si usa maxTransferable per l'ultimo "shot".
        float actualTransfer = Mathf.Min(transferAmount, maxTransferable);

        // Se actualTransfer è positivo, inizia la routine
        if (actualTransfer > 0f)
        {
            StartCoroutine(TransferEnergyRoutine(_closestNPC, actualTransfer));
        }
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
        _currentTransferTarget = npc.transform;

        // Ottieni il VFX Receiver dell'NPC
        NPCVFXReceiver npcVFXReceiver = npc.GetComponent<NPCVFXReceiver>();

        // --- INIZIO ATTIVAZIONE VFX ---

        // 🔥 VFX ON sul Player
        if (transferVFX != null)
            transferVFX.Play();

        // 🔥 VFX ON sull'NPC e avvia rotazione
        if (npcVFXReceiver != null && npcVFXReceiver.incomingVFX != null)
        {
            // Attiva entrambi i sistemi
            npcVFXReceiver.incomingVFX.Play();
            if (npcVFXReceiver.emissionChildVFX != null)
            {
                // Assumendo che emissionChildVFX sia accessibile (o tramite un getter)
                npcVFXReceiver.emissionChildVFX.Play();
            }
            npcVFXReceiver.StartReceivingVFX(); // Avvia la rotazione nell'Update() dell'NPC

            // Imposta il rate iniziale (massimo)
            npcVFXReceiver.UpdateEmissionRate(_playerFullness.CurrentFullness);
        }

        // --- FINE ATTIVAZIONE VFX ---

        // Retrieve dynamic values from EnergyTransferManager
        float npcMultiplier = EnergyTransferManager.Instance.npcMultiplier;
        float playerReturnFraction = EnergyTransferManager.Instance.playerReturnFraction;
        float transferDuration = EnergyTransferManager.Instance.transferDuration;

        float playerStart = _playerFullness.CurrentFullness;
        // Usa actualTransfer come calcolato in HandleTransferRequest
        float playerTarget = Mathf.Clamp(playerStart - actualTransfer, -1f, 1f);

        float npcStart = npc.CurrentFullness;
        float npcTarget = Mathf.Clamp(npcStart + actualTransfer * npcMultiplier, -1f, 1f);

        float elapsed = 0f;
        float currentFullness = playerStart;

        while (elapsed < transferDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transferDuration);

            // Calcola il nuovo Fullness
            currentFullness = Mathf.Lerp(playerStart, playerTarget, t);

            // Applica il Fullness
            _playerFullness.SetFullness(currentFullness);
            npc.SetFullness(Mathf.Lerp(npcStart, npcTarget, t));

            // 🔥 AGGIORNAMENTO DEL RATE OVER TIME SULL'NPC
            if (npcVFXReceiver != null)
            {
                // Passa il valore di Fullness aggiornato per scalare l'emissione del VFX child
                npcVFXReceiver.UpdateEmissionRate(currentFullness);
            }

            yield return null;
        }

        // Finalizza lo stato (assicura il valore target esatto)
        _playerFullness.SetFullness(playerTarget);

        // Calcolo del ritorno (invariato)
        float returnAmount = actualTransfer * npcMultiplier * playerReturnFraction;
        // Aggiorna il Fullness del player dopo il ritorno
        _playerFullness.SetFullness(Mathf.Clamp(_playerFullness.CurrentFullness + returnAmount, -1f, 1f));

        var speedBoost = GetComponent<PlayerSpeedBoost>();
        if (speedBoost != null)
            speedBoost.ActivateBoost();

        _inTransfer = false;
        _currentTransferTarget = null;

        UpdateClosestNPC();

        // --- INIZIO DISATTIVAZIONE VFX ---

        // 🔥 VFX OFF sul Player
        if (transferVFX != null)
            transferVFX.Stop();

        // 🔥 VFX OFF sull'NPC e ferma rotazione
        if (npcVFXReceiver != null)
        {
            // Impostiamo il rate finale (a zero se Fullness è -1)
            // Usiamo il valore Fullness finale PRIMA del ritorno, che è playerTarget
            npcVFXReceiver.UpdateEmissionRate(playerTarget);

            npcVFXReceiver.StopReceivingVFX(); // Ferma la rotazione

            // Ferma entrambi i sistemi
            if (npcVFXReceiver.incomingVFX != null)
                npcVFXReceiver.incomingVFX.Stop();
            if (npcVFXReceiver.emissionChildVFX != null)
                npcVFXReceiver.emissionChildVFX.Stop();
        }
        // --- FINE DISATTIVAZIONE VFX ---
    }

    private void RotateVFXTowardsNPC(Transform npc)
    {
        if (transferVFX == null || npc == null) return;

        Vector3 dir = npc.position - transferVFX.transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transferVFX.transform.rotation = Quaternion.Lerp(
                transferVFX.transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}