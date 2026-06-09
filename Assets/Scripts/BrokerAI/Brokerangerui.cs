using UnityEngine;
using TMPro;

/// <summary>
/// BrokerAngerUI — Displays the broker's anger as a percentage text.
///
/// Listens to EventBus.OnBrokerAngerChanged(int anger).
/// Updates angerText with e.g. "Anger: 35%  [Annoyed]"
/// Optionally colours the text based on tier.
///
/// Setup:
///   Attach to any UI GameObject.
///   Assign angerText (TextMeshProUGUI) in Inspector.
///   Optionally assign a UnityEngine.UI.Slider for a visual bar.
/// </summary>
public class BrokerAngerUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text that shows anger percentage and tier label")]
    public TextMeshProUGUI angerText;

    [Tooltip("Optional slider — set Min=0, Max=100")]
    public UnityEngine.UI.Slider angerSlider;

    [Header("Tier Colours")]
    public Color colourCalm = new Color(0.2f, 0.8f, 0.2f); // green
    public Color colourAnnoyed = new Color(1f, 0.8f, 0.0f); // yellow
    public Color colourAngry = new Color(1f, 0.4f, 0.0f); // orange
    public Color colourFurious = new Color(1f, 0.0f, 0.0f); // red

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void OnEnable() { EventBus.OnBrokerAngerChanged += UpdateUI; }
    void OnDisable() { EventBus.OnBrokerAngerChanged -= UpdateUI; }

    void Start()
    {
        // Show 0% before any event fires
        UpdateUI(0);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void UpdateUI(int anger)
    {
        string tier = GetTierLabel(anger);
        Color col = GetTierColour(anger);

        if (angerText != null)
        {
            angerText.text = $"Anger: {anger}%  [{tier}]";
            angerText.color = col;
        }

        if (angerSlider != null)
            angerSlider.value = anger;
    }

    private string GetTierLabel(int anger)
    {
        if (anger >= 90) return "FURIOUS";
        if (anger >= 60) return "Angry";
        if (anger >= 30) return "Annoyed";
        return "Calm";
    }

    private Color GetTierColour(int anger)
    {
        if (anger >= 90) return colourFurious;
        if (anger >= 60) return colourAngry;
        if (anger >= 30) return colourAnnoyed;
        return colourCalm;
    }
}