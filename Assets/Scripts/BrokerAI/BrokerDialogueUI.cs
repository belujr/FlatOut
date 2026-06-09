using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// BrokerDialogueUI — Shows a speech bubble / dialogue box above the broker.
///
/// Setup:
///   1. Create a World Space Canvas as a child of the Broker GameObject.
///   2. Add a Panel + TextMeshProUGUI inside it.
///   3. Assign dialogueText and dialogueRoot here.
///   4. Call Show("text") from BrokerTaskRunner.
/// </summary>
public class BrokerDialogueUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Root panel of the dialogue bubble — toggled on/off")]
    public GameObject dialogueRoot;

    [Tooltip("The TextMeshPro text inside the bubble")]
    public TextMeshProUGUI dialogueText;

    [Tooltip("How many seconds the dialogue stays visible (default 4)")]
    public float displayDuration = 4f;

    private Coroutine _hideCoroutine;

    void Start()
    {
        Hide();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Show a line of dialogue, then auto-hide after displayDuration.</summary>
    public void Show(string text)
    {
        if (dialogueRoot == null || dialogueText == null)
        {
            Debug.LogWarning("[BrokerDialogueUI] dialogueRoot or dialogueText not assigned.");
            return;
        }

        dialogueText.text = text;
        dialogueRoot.SetActive(true);

        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(AutoHide());

        Debug.Log($"<color=yellow>[BrokerDialogue] {text}</color>");
    }

    /// <summary>Show dialogue and keep it visible until HideAfter() or Hide() is called.</summary>
    public void ShowPersistent(string text)
    {
        if (dialogueRoot == null || dialogueText == null) return;

        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);

        dialogueText.text = text;
        dialogueRoot.SetActive(true);

        Debug.Log($"<color=yellow>[BrokerDialogue] {text}</color>");
    }

    public void Hide()
    {
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        if (dialogueRoot != null) dialogueRoot.SetActive(false);
    }

    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(displayDuration);
        Hide();
    }
}
