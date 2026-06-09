using System.Collections;
using UnityEngine;

/// <summary>
/// BrokerTask_CollectCharge — Broker walks to hall, deducts ₹100, complains.
/// Default tier: Angry (he's mad enough to start charging extra)
///
/// CREATE: Assets > Create > Broker > Tasks > Collect Charge
/// </summary>
[CreateAssetMenu(menuName = "Broker/Tasks/Collect Charge", fileName = "Task_CollectCharge")]
public class BrokerTask_CollectCharge : BrokerTaskSO
{
    [Header("Charge Settings")]
    [Tooltip("Amount deducted via EventBus.OnMoneySpent")]
    public float chargeAmount = 100f;

    private static readonly string[] ChargeReasons =
    {
        "Your neighbours are complaining about the noise. Deducting ₹{0}.",
        "Your house looks very messy. Cleanliness fine — ₹{0}.",
        "Your house is so hot. Ventilation penalty — ₹{0}.",
        "It is maintenance charge time. ₹{0} deducted.",
        "Noise complaints from below again. That will be ₹{0}.",
    };

    public override IEnumerator Execute(BrokerTaskRunner runner)
    {
        if (runner.BrokerAI.hallWaypoint == null)
        {
            Debug.LogWarning("[BrokerTask_CollectCharge] hallWaypoint not assigned on BrokerAI.");
            yield break;
        }

        Debug.Log("<color=green>[BrokerTask_CollectCharge] Walking to hall to collect charge.</color>");
        yield return runner.StartCoroutine(runner.BrokerAI.MoveTo(runner.BrokerAI.hallWaypoint.position));

        string template = ChargeReasons[Random.Range(0, ChargeReasons.Length)];
        string line = string.Format(template, chargeAmount);
        runner.Dialogue.Show(line);

        EventBus.OnMoneySpent?.Invoke(chargeAmount);
        Debug.Log($"<color=green>[BrokerTask_CollectCharge] ₹{chargeAmount} deducted via EventBus.OnMoneySpent.</color>");

        yield return new WaitForSeconds(runner.Dialogue.displayDuration);
    }
}
