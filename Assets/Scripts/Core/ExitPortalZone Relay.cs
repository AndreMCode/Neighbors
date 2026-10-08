using UnityEngine;

/// <summary>
/// ExitPortalZoneRelay is a Unity MonoBehaviour that relays trigger events to an associated ExitPortal component.
/// It differentiates between inner and outer zones, allowing for specific handling of trigger events based on the zone type.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ExitPortalZoneRelay : MonoBehaviour
{
    public enum ZoneType { Inner, Outer }

    [SerializeField] private ZoneType zoneType;
    [SerializeField] private ExitPortal exitPortal;

    private void Reset()
    {
        exitPortal = GetComponentInParent<ExitPortal>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (exitPortal == null || zoneType != ZoneType.Inner) return;

        exitPortal.HandleInnerTriggerEnter(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (exitPortal == null || zoneType != ZoneType.Outer) return;

        exitPortal.HandleOuterTriggerStay(collision);
    }
}