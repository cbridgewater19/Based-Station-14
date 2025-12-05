using Content.Goobstation.Shared.ItemMiner;
using Content.Goobstation.Server.ItemMiner;
using Content.Goobstation.Common.ItemMiner;
using Content.Shared.CCVar;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.ItemMiner;

public sealed class TelecrystalMinerSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelecrystalMinerComponent, ItemMinedEvent>(OnItemMined);
    }

    private void OnItemMined(EntityUid uid, TelecrystalMinerComponent component, ItemMinedEvent args)
    {
        // Get the current count of telecrystals in the slot
        if (!TryComp<ItemMinerComponent>(uid, out var itemMiner))
            return;

        // Calculate total count based on the mined event and existing items
        var totalCount = args.Count;

        // Try to get the current count from the item slot if it exists
        if (itemMiner.ItemSlotId != null && _itemSlots.TryGetSlot(uid, itemMiner.ItemSlotId, out var slot))
        {
            if (slot.Item != null && TryComp<StackComponent>(slot.Item.Value, out var stack))
            {
                totalCount = stack.Count;
            }
        }

        // Announce when we reach the announce threshold
        if (totalCount >= component.AnnounceAt && !component.HasAnnounced)
        {
            component.HasAnnounced = true;
            AnnounceTelecrystalMiner(uid, totalCount);
        }

        // Reveal location when we reach the location threshold
        if (totalCount >= component.LocationAt && !component.HasRevealedLocation)
        {
            component.HasRevealedLocation = true;
            RevealTelecrystalMinerLocation(uid, totalCount);
        }
    }

    private void AnnounceTelecrystalMiner(EntityUid uid, int count)
    {
        var message = $"A telecrystal miner has generated {count} telecrystals!";
        // You can implement your own announcement system here
        // For now, we'll just log it
        Logger.Info(message);
    }

    private void RevealTelecrystalMinerLocation(EntityUid uid, int count)
    {
        var transform = Transform(uid);
        var coordinates = transform.Coordinates;

        var message = $"A telecrystal miner has generated {count} telecrystals at {coordinates}!";
        // You can implement your own location reveal system here
        // For now, we'll just log it
        Logger.Info(message);
    }
}
