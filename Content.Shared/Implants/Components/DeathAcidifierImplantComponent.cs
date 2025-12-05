// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared.Implants.Components;

/// <summary>
/// Marker component for the death acidifier implant.
/// This implant allows the user to manually activate it to spawn an acid puddle and gib themselves.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DeathAcidifierImplantComponent : Component
{
}
