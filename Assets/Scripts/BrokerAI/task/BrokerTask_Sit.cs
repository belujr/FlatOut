using System.Collections;
using UnityEngine;

/// <summary>
/// BrokerTask_Sit — Broker walks to random furniture, sits for 5 seconds, says tired line.
/// Default tier: Calm (broker is relaxed, just having a look around)
///
/// CREATE: Assets > Create > Broker > Tasks > Sit At Furniture
/// </summary>
[CreateAssetMenu(menuName = "Broker/Tasks/Sit At Furniture", fileName = "Task_SitAtFurniture")]
public class BrokerTask_Sit : BrokerTaskSO
{
    [Header("Sit Settings")]
    [Tooltip("How long the broker sits before getting up")]
    public float sitDuration = 5f;

    private static readonly string[] TiredLines =
    {
        "Wow... I am sitting here, feeling so tired.",
        "Ah, finally a place to rest. My feet are killing me.",
        "These stairs are too much. Let me just sit for a moment.",
    };

    public override IEnumerator Execute(BrokerTaskRunner runner)
    {
        GameObject[] furnitureObjects = GameObject.FindGameObjectsWithTag("furniture");

        if (furnitureObjects.Length == 0)
        {
            Debug.LogWarning("[BrokerTask_Sit] No GameObjects tagged 'furniture' found.");
            yield break;
        }

        GameObject target = furnitureObjects[Random.Range(0, furnitureObjects.Length)];

        Debug.Log($"<color=green>[BrokerTask_Sit] Walking to furniture: {target.name}</color>");
        yield return runner.StartCoroutine(runner.BrokerAI.MoveTo(target.transform.position));

        string line = TiredLines[Random.Range(0, TiredLines.Length)];
        runner.Dialogue.ShowPersistent(line);

        yield return new WaitForSeconds(sitDuration);

        runner.Dialogue.Hide();
    }
}
