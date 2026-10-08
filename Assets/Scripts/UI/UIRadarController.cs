using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIRadarController : MonoBehaviour
{
    [Header("UI Document References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string radarContainerName = "radar-frame";

    [Header("Radar Tuning")]
    [SerializeField] private float radarScale = 10f;         // Pixels per world unit
    [SerializeField] private float maxDisplayRadius = 135f;  // Max radius in pixels from center
    [SerializeField] private bool clampToEdge = true;        // Keep blips pinned to edge if out of range

    [Header("Blip Sizing")]
    [SerializeField] private float clampedBlipSize = 6f; // Smaller when on the edge
    [SerializeField] private float inRangeBlipSize = 12f; // Larger when nearby

    [Header("Optimization")]
    [SerializeField] private float updateInterval = 0.033f; // ~30 updates per second

    private VisualElement radarFrame;
    private Transform playerTransform;
    private List<VictimComponent> toRemove = new();
    private readonly Dictionary<VictimComponent, VisualElement> activeBlips = new();

    private float timeSinceLastUpdate = 0f;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

        VisualElement root = uiDocument.rootVisualElement;
        radarFrame = root.Q<VisualElement>(radarContainerName);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (radarFrame == null || playerTransform == null || GameStateManager.Instance == null) return;

        // Throttle updates to save CPU performance
        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate >= updateInterval)
        {
            timeSinceLastUpdate = 0f;
            SyncBlips();
            UpdateBlipPositions();
        }
    }

    private void SyncBlips()
    {
        IReadOnlyList<VictimComponent> currentVictims = GameStateManager.Instance.ActiveVictims;

        // Remove blips for victims that were rescued or destroyed
        toRemove.Clear();
        foreach (var kvp in activeBlips)
        {
            if (kvp.Key == null || !kvp.Key.gameObject.activeInHierarchy)
            {
                radarFrame.Remove(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        for (int i = 0; i < toRemove.Count; i++)
        {
            activeBlips.Remove(toRemove[i]);
        }

        // Add blips for newly spawned victims
        for (int i = 0; i < currentVictims.Count; i++)
        {
            VictimComponent victim = currentVictims[i];
            if (victim != null && victim.gameObject.activeInHierarchy && !activeBlips.ContainsKey(victim))
            {
                VisualElement blip = CreateBlipElement();
                radarFrame.Add(blip);
                activeBlips.Add(victim, blip);
            }
        }
    }

    private VisualElement CreateBlipElement()
    {
        VisualElement blip = new();
        blip.AddToClassList("radar-blip");

        // Fallback styling
        blip.style.position = Position.Absolute;

        // Initialize with the clamped size
        blip.style.width = clampedBlipSize;
        blip.style.height = clampedBlipSize;
        blip.style.backgroundColor = Color.yellow;

        float initialRadius = clampedBlipSize / 2f;
        blip.style.borderTopLeftRadius = initialRadius;
        blip.style.borderTopRightRadius = initialRadius;
        blip.style.borderBottomLeftRadius = initialRadius;
        blip.style.borderBottomRightRadius = initialRadius;

        return blip;
    }

    private void UpdateBlipPositions()
    {
        Vector2 playerPos = playerTransform.position;

        // Calculate center of radar frame (fallback to default radius if layout hasn't computed)
        float centerX = radarFrame.resolvedStyle.width > 0 ? radarFrame.resolvedStyle.width / 2f : maxDisplayRadius;
        float centerY = radarFrame.resolvedStyle.height > 0 ? radarFrame.resolvedStyle.height / 2f : maxDisplayRadius;

        foreach (var kvp in activeBlips)
        {
            VictimComponent victim = kvp.Key;
            VisualElement blip = kvp.Value;

            Vector2 deltaWorld = (Vector2)victim.transform.position - playerPos;

            // UI Toolkit Y-axis is top-down (inverted relative to Unity 2D world Y-axis)
            Vector2 radarOffset = new Vector2(deltaWorld.x, -deltaWorld.y) * radarScale;

            bool isClamped = clampToEdge && radarOffset.magnitude > maxDisplayRadius;
            float currentSize = isClamped ? clampedBlipSize : inRangeBlipSize;

            if (isClamped)
            {
                radarOffset = radarOffset.normalized * maxDisplayRadius;
            }

            // Update size and border radii dynamically based on range
            blip.style.width = currentSize;
            blip.style.height = currentSize;

            float radius = currentSize / 2f;
            blip.style.borderTopLeftRadius = radius;
            blip.style.borderTopRightRadius = radius;
            blip.style.borderBottomLeftRadius = radius;
            blip.style.borderBottomRightRadius = radius;

            // Offset by half the dynamic blip size so it remains perfectly centered
            blip.style.left = centerX + radarOffset.x - radius;
            blip.style.top = centerY + radarOffset.y - radius;
        }
    }
}