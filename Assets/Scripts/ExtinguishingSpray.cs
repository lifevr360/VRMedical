using UnityEngine;

/// <summary>
/// Marks a particle system as an extinguisher spray, so ExtinguishableFire reacts to it
/// and ignores hits from any other particle system (fire, smoke, ...).
/// FireExtinguisherLever adds this to its smoke effect automatically -- no need to add it by hand.
/// </summary>
public class ExtinguishingSpray : MonoBehaviour
{
}
