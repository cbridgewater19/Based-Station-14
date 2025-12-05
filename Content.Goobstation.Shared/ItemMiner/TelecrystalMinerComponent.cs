using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.ItemMiner;

[RegisterComponent, NetworkedComponent]
public sealed partial class TelecrystalMinerComponent : Component
{
    /// <summary>
    /// How many telecrystals to announce at
    /// </summary>
    [DataField("announceAt")]
    public int AnnounceAt = 40;

    /// <summary>
    /// How many telecrystals to announce location at
    /// </summary>
    [DataField("locationAt")]
    public int LocationAt = 100;

    /// <summary>
    /// Whether we've already announced at the announce threshold
    /// </summary>
    public bool HasAnnounced = false;

    /// <summary>
    /// Whether we've already revealed location at the location threshold
    /// </summary>
    public bool HasRevealedLocation = false;
}
