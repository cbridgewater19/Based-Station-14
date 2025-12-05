// SPDX-FileCopyrightText: 2025 Based Station
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;

namespace Content.Server.Hands.Systems;

/// <summary>
/// Server-side implementation of H.A.R.M.P.A.C.K system
/// </summary>
public sealed class HarmPackSystem : Content.Shared.Hands.EntitySystems.HarmPackSystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            Logger.Info("H.A.R.M.P.A.C.K Server System initializing...");

            SubscribeLocalEvent<HarmPackComponent, ComponentStartup>(OnHarmPackStartup);
            SubscribeLocalEvent<HarmPackComponent, ComponentShutdown>(OnHarmPackShutdown);
            SubscribeLocalEvent<HarmPackComponent, GotEquippedEvent>(OnHarmPackEquipped);
            SubscribeLocalEvent<HarmPackComponent, GotUnequippedEvent>(OnHarmPackUnequipped);

            Logger.Info("H.A.R.M.P.A.C.K Server System initialized");
        }

    private void OnHarmPackStartup(EntityUid uid, HarmPackComponent component, ComponentStartup args)
    {
        // Initialize the H.A.R.M.P.A.C.K when it's first added
        component.IsActive = false;
        component.NextSwapTime = TimeSpan.Zero;
    }

    private void OnHarmPackShutdown(EntityUid uid, HarmPackComponent component, ComponentShutdown args)
    {
        // Clean up extra hands when the component is removed
        if (TryComp<HandsComponent>(uid, out var hands))
        {
            RemoveExtraHands(uid, component, hands);
        }
    }

        private void OnHarmPackEquipped(EntityUid uid, HarmPackComponent component, GotEquippedEvent args)
        {
            Logger.Info($"H.A.R.M.P.A.C.K equipped by {ToPrettyString(args.Equipee)}");

            // Add HarmPackComponent to the wearer so they can use the keybind
            var harmPackComp = EnsureComp<HarmPackComponent>(args.Equipee);
            harmPackComp.IsActive = false; // Start with normal hands active
            harmPackComp.NextSwapTime = TimeSpan.Zero;

            // Always add the extra hands to the UI (they'll be greyed out when inactive)
            if (TryComp<HandsComponent>(args.Equipee, out var hands))
            {
                Logger.Info($"Adding extra hands to {ToPrettyString(args.Equipee)}");
                AddExtraHands(args.Equipee, harmPackComp, hands);
            }
        }

        private void OnHarmPackUnequipped(EntityUid uid, HarmPackComponent component, GotUnequippedEvent args)
        {
            Logger.Info($"H.A.R.M.P.A.C.K unequipped by {ToPrettyString(args.Equipee)}");

            // Remove any extra hands that were added
            if (TryComp<HandsComponent>(args.Equipee, out var hands))
            {
                // Find any H.A.R.M.P.A.C.K items that might still be equipped
                var hasHarmPack = false;
                if (_inventorySystem.TryGetSlots(args.Equipee, out var slots))
                {
                    foreach (var slot in slots)
                    {
                        if (_inventorySystem.TryGetSlotEntity(args.Equipee, slot.Name, out var slotEntity) &&
                            slotEntity != null && HasComp<HarmPackComponent>(slotEntity.Value))
                        {
                            hasHarmPack = true;
                            break;
                        }
                    }
                }

                if (!hasHarmPack)
                {
                    // Remove extra hands if no H.A.R.M.P.A.C.K is equipped
                    foreach (var handName in new[] { "harm_left", "harm_right" })
                    {
                        if (hands.Hands.ContainsKey(handName))
                        {
                            if (hands.Hands[handName].HeldEntity != null)
                            {
                                _handsSystem.TryDrop(args.Equipee, hands.Hands[handName], handsComp: hands);
                            }
                            _handsSystem.RemoveHand(args.Equipee, handName, hands);
                        }
                    }
                }
            }
        }

    protected override void UpdateHarmPackUI(EntityUid uid, HarmPackComponent harmPack)
    {
        // Server doesn't need to update UI directly
        // This will be handled by the client-side system
    }
}
