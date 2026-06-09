using System.Collections;
using UnityEngine;

/// <summary>
/// BrokerTask_Kitchen — Broker walks to kitchen, says hungry/thirsty line.
/// Default tier: Calm
///
/// CREATE: Assets > Create > Broker > Tasks > Go To Kitchen
/// </summary>
[CreateAssetMenu(menuName = "Broker/Tasks/Go To Kitchen", fileName = "Task_GoToKitchen")]
public class BrokerTask_Kitchen : BrokerTaskSO
{
    private static readonly string[] FoodLines =
    {
        "Oh, I am hungry. Seeing if there is something to eat...",
        "Hey, I am thirsty. I need a glass of water.",
        "All this walking made me hungry. Any food around here?",
    };

    public override IEnumerator Execute(BrokerTaskRunner runner)
    {
        if (runner.BrokerAI.kitchenWaypoint == null)
        {
            Debug.LogWarning("[BrokerTask_Kitchen] kitchenWaypoint not assigned on BrokerAI.");
            yield break;
        }

        Debug.Log("<color=green>[BrokerTask_Kitchen] Walking to kitchen.</color>");
        yield return runner.StartCoroutine(runner.BrokerAI.MoveTo(runner.BrokerAI.kitchenWaypoint.position));

        string line = FoodLines[Random.Range(0, FoodLines.Length)];
        runner.Dialogue.Show(line);

        yield return new WaitForSeconds(runner.Dialogue.displayDuration);
    }
}
