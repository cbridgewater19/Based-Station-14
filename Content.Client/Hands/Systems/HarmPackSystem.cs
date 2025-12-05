// SPDX-FileCopyrightText: 2025 Based Station
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.Client.Hands.Systems;

/// <summary>
/// Client-side implementation of H.A.R.M.P.A.C.K system
/// </summary>
public sealed class HarmPackSystem : Content.Shared.Hands.EntitySystems.HarmPackSystem
{
    public override void Initialize()
    {
        base.Initialize();

        Logger.Info("H.A.R.M.P.A.C.K Client System initializing...");

        SubscribeLocalEvent<HarmPackComponent, ComponentStartup>(OnHarmPackStartup);
        SubscribeLocalEvent<HarmPackComponent, ComponentShutdown>(OnHarmPackShutdown);

        Logger.Info("H.A.R.M.P.A.C.K Client System initialized");
    }

    private void OnHarmPackStartup(EntityUid uid, HarmPackComponent component, ComponentStartup args)
    {
        // Initialize UI elements for the H.A.R.M.P.A.C.K
        UpdateHarmPackUI(uid, component);
    }

    private void OnHarmPackShutdown(EntityUid uid, HarmPackComponent component, ComponentShutdown args)
    {
        // Clean up UI elements when the component is removed
        UpdateHarmPackUI(uid, component);
    }

    protected override void UpdateHarmPackUI(EntityUid uid, HarmPackComponent harmPack)
    {
        // Update the hands UI to show/hide extra hands
        // This will be handled by the existing hands UI system
        // The extra hands will automatically appear/disappear based on the HandsComponent state
    }
}
