// SPDX-FileCopyrightText: 2025 Based Station
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Containers;
using Content.Shared.Inventory;

namespace Content.Shared.Hands.EntitySystems;

/// <summary>
/// System for managing H.A.R.M.P.A.C.K functionality
/// </summary>
public abstract partial class HarmPackSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            Logger.Info("H.A.R.M.P.A.C.K System initializing...");

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.SwapHandSets, InputCmdHandler.FromDelegate(HandleSwapHandSets, handle: false, outsidePrediction: false))
                .Register<HarmPackSystem>();

            Logger.Info("H.A.R.M.P.A.C.K System initialized and keybind registered");
        }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<HarmPackSystem>();
    }

        /// <summary>
        /// Handles the keybind for swapping between hand sets
        /// </summary>
        private void HandleSwapHandSets(ICommonSession? session)
        {
            if (session?.AttachedEntity == null)
            {
                Logger.Warning("H.A.R.M.P.A.C.K keybind pressed but no attached entity");
                return;
            }

            Logger.Info($"H.A.R.M.P.A.C.K keybind pressed by {ToPrettyString(session.AttachedEntity.Value)}");
            var result = TrySwapHandSets(session.AttachedEntity.Value);
            Logger.Info($"H.A.R.M.P.A.C.K keybind result: {result}");
        }

    /// <summary>
    /// Attempts to swap between normal hands and H.A.R.M.P.A.C.K hands
    /// </summary>
    public bool TrySwapHandSets(EntityUid uid)
    {
        Logger.Info($"TrySwapHandSets called for {ToPrettyString(uid)}");

        // Check if the player has a HarmPackComponent (added when they equip a H.A.R.M.P.A.C.K)
        if (!TryComp<HarmPackComponent>(uid, out var harmPackComp))
        {
            Logger.Warning($"No HarmPackComponent found on {ToPrettyString(uid)}");
            return false;
        }

        if (!TryComp<HandsComponent>(uid, out var hands))
        {
            Logger.Warning($"No HandsComponent found on {ToPrettyString(uid)}");
            return false;
        }

        // Check cooldown
        if (harmPackComp.NextSwapTime > _gameTiming.CurTime)
        {
            Logger.Info($"H.A.R.M.P.A.C.K on cooldown for {ToPrettyString(uid)}");
            return false;
        }

        // Toggle the active state
        harmPackComp.IsActive = !harmPackComp.IsActive;
        Logger.Info($"H.A.R.M.P.A.C.K active state toggled to {harmPackComp.IsActive} for {ToPrettyString(uid)}");

        if (harmPackComp.IsActive)
        {
            // Switch to extra hands
            Logger.Info($"Switching to extra hands for {ToPrettyString(uid)}");
            AddExtraHands(uid, harmPackComp, hands);
        }
        else
        {
            // Switch to normal hands
            Logger.Info($"Switching to normal hands for {ToPrettyString(uid)}");
            RemoveExtraHands(uid, harmPackComp, hands);
        }

        // Set cooldown
        harmPackComp.NextSwapTime = _gameTiming.CurTime + harmPackComp.SwapCooldown;

        // Notify client
        if (_net.IsClient)
        {
            // Client-side UI update
            UpdateHarmPackUI(uid, harmPackComp);
        }

        return true;
    }

    /// <summary>
    /// Adds the extra hands provided by the H.A.R.M.P.A.C.K
    /// </summary>
    protected void AddExtraHands(EntityUid uid, HarmPackComponent harmPack, HandsComponent hands)
    {
        // Add 2 extra hands: 1 left, 1 right (alongside normal hands)
        var extraHands = new[]
        {
            ("harm_left", HandLocation.Left),
            ("harm_right", HandLocation.Right)
        };

        foreach (var (handName, location) in extraHands)
        {
            if (!hands.Hands.ContainsKey(handName))
            {
                _handsSystem.AddHand(uid, handName, location, hands);
            }
        }
    }

    /// <summary>
    /// Removes the extra hands provided by the H.A.R.M.P.A.C.K
    /// </summary>
    protected void RemoveExtraHands(EntityUid uid, HarmPackComponent harmPack, HandsComponent hands)
    {
        // Remove the 2 extra hands
        var extraHandNames = new[] { "harm_left", "harm_right" };

        foreach (var handName in extraHandNames)
        {
            if (hands.Hands.ContainsKey(handName))
            {
                // Drop any items in the extra hands before removing them
                if (hands.Hands[handName].HeldEntity != null)
                {
                    _handsSystem.TryDrop(uid, hands.Hands[handName], handsComp: hands);
                }
                _handsSystem.RemoveHand(uid, handName, hands);
            }
        }
    }

    /// <summary>
    /// Updates the UI to reflect the current H.A.R.M.P.A.C.K state
    /// </summary>
    protected abstract void UpdateHarmPackUI(EntityUid uid, HarmPackComponent harmPack);

    /// <summary>
    /// Checks if the H.A.R.M.P.A.C.K is equipped and active
    /// </summary>
    public bool IsHarmPackActive(EntityUid uid)
    {
        return TryComp<HarmPackComponent>(uid, out var harmPack) && harmPack.IsActive;
    }
}
