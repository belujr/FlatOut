using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// CalendarManager — Day & Month Cycle + UI Display
///
/// Attach to any GameObject in your scene.
/// Listens to EventBus.OnTimeTick (fired by your GameManager every in-game hour).
/// Detects midnight rollover → advances the day → advances the month every 15 days.
///
/// UI is built-in: assign the TextMeshPro references in the Inspector.
/// A "New Day" and "New Month" banner can optionally pop up on screen.
/// </summary>
public class CalendarManager : MonoBehaviour
{
    // ── Calendar Settings ─────────────────────────────────────────────────────

    [Header("Calendar Settings")]
    [Tooltip("How many in-game days make one month")]
    public int daysPerMonth = 15;

    [Tooltip("Starting day (1-based)")]
    public int startDay = 1;

    [Tooltip("Starting month (1-based)")]
    public int startMonth = 1;

    // ── UI References ─────────────────────────────────────────────────────────

    [Header("UI — Date Display")]
    [Tooltip("Persistent HUD text showing current day and month, e.g. 'Day 3  •  Month 2'")]
    public TextMeshProUGUI dateText;

    [Header("UI — New Day Banner")]
    [Tooltip("Root GameObject of the 'New Day' banner panel (will be toggled on/off)")]
    public GameObject newDayBannerRoot;

    [Tooltip("Text inside the New Day banner, e.g. 'Day 4'")]
    public TextMeshProUGUI newDayBannerText;

    [Tooltip("How many real seconds the New Day banner stays visible")]
    public float newDayBannerDuration = 2.5f;

    [Header("UI — New Month Banner")]
    [Tooltip("Root GameObject of the 'New Month' banner panel (will be toggled on/off)")]
    public GameObject newMonthBannerRoot;

    [Tooltip("Text inside the New Month banner, e.g. 'Month 3 Begins'")]
    public TextMeshProUGUI newMonthBannerText;

    [Tooltip("How many real seconds the New Month banner stays visible")]
    public float newMonthBannerDuration = 4f;

    // ── Runtime State ─────────────────────────────────────────────────────────

    public int CurrentDay { get; private set; }
    public int CurrentMonth { get; private set; }

    /// <summary>Total in-game days elapsed since game start (useful for quests/save data).</summary>
    public int TotalDaysElapsed { get; private set; }

    // Used to detect midnight: when OnTimeTick fires hour == 0 after a non-zero hour
    private int _lastHour = -1;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        CurrentDay = startDay;
        CurrentMonth = startMonth;
        TotalDaysElapsed = 0;
    }

    void Start()
    {
        // Hide banners at startup — they should be invisible by default
        SetBannerVisible(newDayBannerRoot, false);
        SetBannerVisible(newMonthBannerRoot, false);

        // Populate the HUD immediately so it isn't blank on frame 1
        RefreshDateText();
    }

    void OnEnable()
    {
        EventBus.OnTimeTick += HandleHourTick;
    }

    void OnDisable()
    {
        EventBus.OnTimeTick -= HandleHourTick;
    }

    // ── Core Clock Logic ──────────────────────────────────────────────────────

    /// <summary>
    /// Called every in-game hour by GameManager via EventBus.OnTimeTick.
    /// Detects midnight (currentHour wraps to 0) to trigger a new day.
    /// </summary>
    private void HandleHourTick(int currentHour)
    {
        // Midnight: clock just rolled from 23 → 0
        bool isMidnight = (currentHour == 0 && _lastHour > 0);

        if (isMidnight)
        {
            AdvanceDay();
        }

        _lastHour = currentHour;
    }

    /// <summary>
    /// Increments the day, rolls month when daysPerMonth is reached,
    /// fires EventBus events, and triggers UI banners.
    /// </summary>
    private void AdvanceDay()
    {
        CurrentDay++;
        TotalDaysElapsed++;

        bool newMonth = false;

        if (CurrentDay > daysPerMonth)
        {
            CurrentDay = 1;
            CurrentMonth++;
            newMonth = true;

            // Fire month event BEFORE day event so listeners receive month first
            EventBus.OnMonthChanged?.Invoke(CurrentMonth);
        }

        EventBus.OnDayChanged?.Invoke(CurrentDay, CurrentMonth);

        // Update persistent HUD
        RefreshDateText();

        // Show banners (month banner shown instead of day banner on rollover)
        if (newMonth)
            ShowNewMonthBanner();
        else
            ShowNewDayBanner();

        Debug.Log($"[Calendar] Day {CurrentDay} / Month {CurrentMonth}  (Total days: {TotalDaysElapsed})");
    }

    // ── UI Methods ────────────────────────────────────────────────────────────

    /// <summary>Updates the persistent HUD date text.</summary>
    private void RefreshDateText()
    {
        if (dateText == null) return;
        dateText.text = $"Day {CurrentDay}  •  Month {CurrentMonth}";
    }

    /// <summary>Pops the "New Day" banner briefly, then hides it.</summary>
    private void ShowNewDayBanner()
    {
        if (newDayBannerRoot == null) return;

        if (newDayBannerText != null)
            newDayBannerText.text = $"Day {CurrentDay}";

        StopCoroutine(nameof(HideBannerAfterDelay_Day)); // cancel if already running
        StartCoroutine(HideBannerAfterDelay_Day(newDayBannerDuration));
    }

    /// <summary>Pops the "New Month" banner briefly, then hides it.</summary>
    private void ShowNewMonthBanner()
    {
        if (newMonthBannerRoot == null) return;

        // Hide the day banner in case it was already showing
        SetBannerVisible(newDayBannerRoot, false);

        if (newMonthBannerText != null)
            newMonthBannerText.text = $"Month {CurrentMonth} Begins";

        StopCoroutine(nameof(HideBannerAfterDelay_Month));
        StartCoroutine(HideBannerAfterDelay_Month(newMonthBannerDuration));
    }

    private IEnumerator HideBannerAfterDelay_Day(float delay)
    {
        SetBannerVisible(newDayBannerRoot, true);
        yield return new WaitForSeconds(delay);
        SetBannerVisible(newDayBannerRoot, false);
    }

    private IEnumerator HideBannerAfterDelay_Month(float delay)
    {
        SetBannerVisible(newMonthBannerRoot, true);
        yield return new WaitForSeconds(delay);
        SetBannerVisible(newMonthBannerRoot, false);
    }

    private void SetBannerVisible(GameObject bannerRoot, bool visible)
    {
        if (bannerRoot != null)
            bannerRoot.SetActive(visible);
    }

    // ── Public Helpers ────────────────────────────────────────────────────────

    /// <summary>Returns formatted date string. Use anywhere in code.</summary>
    public string GetFormattedDate() => $"Day {CurrentDay}, Month {CurrentMonth}";
}