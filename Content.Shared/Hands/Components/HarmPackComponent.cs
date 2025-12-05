// SPDX-FileCopyrightText: 2025 Based Station
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;
using Robust.Shared.Serialization;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.Hands.Components;

/// <summary>
/// Component for the H.A.R.M.P.A.C.K (Hands Augmentation and Remote Manipulation Pack)
/// Provides an extra set of hands that can be swapped between using a keybind.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedHandsSystem))]
public sealed partial class HarmPackComponent : Component
{
    /// <summary>
    /// Whether the H.A.R.M.P.A.C.K is currently active (extra hands are available)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsActive = false;

    /// <summary>
    /// The names of the extra hands provided by the H.A.R.M.P.A.C.K
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public List<string> ExtraHandNames = new() { "harm_left", "harm_right" };

    /// <summary>
    /// Whether the extra hands are currently visible in the UI
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ShowExtraHands = true;

    /// <summary>
    /// Cooldown time between hand set swaps to prevent spam
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan SwapCooldown = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// The time at which the next hand set swap will be allowed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextSwapTime;
}
