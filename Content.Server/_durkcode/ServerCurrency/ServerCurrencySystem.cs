// SPDX-FileCopyrightText: 2024 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 SX-7 <92227810+SX-7@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Popups;
using Content.Server.Shuttles.Systems;
using Content.Shared._durkcode.ServerCurrency;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Content.Server._RMC14.LinkAccount;
using Robust.Shared.Log;
using Robust.Shared.Network;

namespace Content.Server._durkcode.ServerCurrency
{
    /// <summary>
    /// Connects <see cref="ServerCurrencyManager"/> to the simulation state.
    /// </summary>
    public sealed class ServerCurrencySystem : EntitySystem
    {
        [Dependency] private readonly ServerCurrencyManager _currencyMan = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly SharedJobSystem _jobs = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly LinkAccountManager _linkAccount = default!;
        [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;

        private ISawmill _sawmill = default!;

        private int _penniesPerPlayer = 10;
        private int _penniesNonAntagMultiplier = 1;
        private int _penniesServerMultiplier = 1;
        private int _penniesMinPlayers;
        private int _penniesEvacuationMultiplier = 3;
        private float _penniesGreentextMultiplier = 1.5f;

        // Track players who completed their objectives (greentext)
        private HashSet<NetUserId> _greentextCompletedPlayers = new();

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = Logger.GetSawmill("currency");
            _currencyMan.BalanceChange += OnBalanceChange;
            SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
            SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
            SubscribeNetworkEvent<PlayerBalanceRequestEvent>(OnBalanceRequest);
            Subs.CVar(_cfg, GoobCVars.PenniesPerPlayer, value => _penniesPerPlayer = value, true);
            Subs.CVar(_cfg, GoobCVars.PennyNonAntagMultiplier, value => _penniesNonAntagMultiplier = value, true);
            Subs.CVar(_cfg, GoobCVars.PennyServerMultiplier, value => _penniesServerMultiplier = value, true);
            Subs.CVar(_cfg, GoobCVars.PennyMinPlayers, value => _penniesMinPlayers = value, true);
            Subs.CVar(_cfg, GoobCVars.PennyEvacuationMultiplier, value => _penniesEvacuationMultiplier = value, true);
            Subs.CVar(_cfg, GoobCVars.PennyGreentextMultiplier, value => _penniesGreentextMultiplier = value, true);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _currencyMan.BalanceChange -= OnBalanceChange;
        }



        private void OnRoundEndText(RoundEndTextAppendEvent ev)
        {
            _sawmill.Info($"Round ended. Player count: {_players.PlayerCount}, Min required: {_penniesMinPlayers}");
            if (_players.PlayerCount < _penniesMinPlayers)
            {
                _sawmill.Info("Not enough players for penny distribution - skipping");
                return;
            }

            var query = EntityQueryEnumerator<MindContainerComponent>();

            while (query.MoveNext(out var uid, out var mindContainer))
            {
                var isBorg = HasComp<BorgChassisComponent>(uid);
                if (!(HasComp<HumanoidAppearanceComponent>(uid)
                    || HasComp<BorgBrainComponent>(uid)
                    || isBorg))
                    continue;

                if (mindContainer.Mind.HasValue)
                {
                    var mind = Comp<MindComponent>(mindContainer.Mind.Value);
                    if (mind is not null
                        && (isBorg || !_mind.IsCharacterDeadIc(mind)) // Borgs count always as dead so I'll just throw them a bone and give them an exception.
                        && mind.OriginalOwnerUserId.HasValue
                        && _players.TryGetSessionById(mind.UserId, out var session))
                    {
                        int money = _penniesPerPlayer;
                        _sawmill.Info($"Starting penny calculation for player {mind.OriginalOwnerUserId.Value}: base={money}");
                        if (session is not null)
                        {
                            var jobPennies = _jobs.GetJobPennies(session);
                            money += jobPennies;
                            _sawmill.Info($"Added job pennies: {jobPennies}, total now: {money}");

                            var canBeAntag = _jobs.CanBeAntag(session);
                            _sawmill.Info($"Player can be antag: {canBeAntag}");
                            if (!canBeAntag)
                            {
                                money *= _penniesNonAntagMultiplier;
                                _sawmill.Info($"Applied non-antag multiplier {_penniesNonAntagMultiplier}, total now: {money}");
                            }
                        }

                        if (_penniesServerMultiplier != 1)
                            money *= _penniesServerMultiplier;

                        // Patron bonus system - disabled for now, but kept for future use
                        // if (session != null && _linkAccount.GetPatron(session)?.Tier != null)
                        //     money *= 2;

                        // Give bonus for successful evacuation to Central Command
                        // Check if the player is currently escaping on the emergency shuttle
                        _sawmill.Info($"Checking evacuation status for player {mind.OriginalOwnerUserId.Value}: has entity={mind.OwnedEntity.HasValue}");
                        if (mind.OwnedEntity.HasValue)
                        {
                            var isEscaping = _emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value);
                            _sawmill.Info($"Player {mind.OriginalOwnerUserId.Value} isEscaping={isEscaping}");
                            if (isEscaping)
                            {
                                money *= _penniesEvacuationMultiplier;
                                _sawmill.Info($"Applying {_penniesEvacuationMultiplier}x evacuation bonus to player {mind.OriginalOwnerUserId.Value} (final amount: {money} pennies)");
                            }
                        }

                        // Apply greentext multiplier if player completed their objectives
                        if (mind.OriginalOwnerUserId.HasValue && _greentextCompletedPlayers.Contains(mind.OriginalOwnerUserId.Value))
                        {
                            money = (int)(money * _penniesGreentextMultiplier);
                            _sawmill.Info($"Applied greentext multiplier {_penniesGreentextMultiplier}x to player {mind.OriginalOwnerUserId.Value} (final amount: {money} pennies)");
                        }

                        _sawmill.Info($"Final penny amount for player {mind.OriginalOwnerUserId.Value}: {money}");
                        _currencyMan.AddCurrency(mind.OriginalOwnerUserId.Value, money);
                    }
                }
            }
        }

        private void OnRoundStart(RoundStartingEvent ev)
        {
            // Clear the greentext completion list at the start of each round
            _greentextCompletedPlayers.Clear();
            _sawmill.Info("Cleared greentext completion list for new round");
        }

        /// <summary>
        /// Registers that a player has completed their objectives (greentext)
        /// </summary>
        public void RegisterGreentextCompletion(NetUserId userId)
        {
            _greentextCompletedPlayers.Add(userId);
            _sawmill.Info($"Registered greentext completion for player {userId}");
        }

        private void OnBalanceRequest(PlayerBalanceRequestEvent ev, EntitySessionEventArgs eventArgs)
        {
            var senderSession = eventArgs.SenderSession;
            var balance = _currencyMan.GetBalance(senderSession.UserId);
            RaiseNetworkEvent(new PlayerBalanceUpdateEvent(balance, balance), senderSession);

        }

        /// <summary>
        /// Calls event that when a player's balance is updated.
        /// Also handles popups
        /// </summary>
        private void OnBalanceChange(PlayerBalanceChangeEvent ev)
        {
            RaiseNetworkEvent(new PlayerBalanceUpdateEvent(ev.NewBalance, ev.OldBalance), ev.UserSes);


            if (ev.UserSes.AttachedEntity.HasValue)
            {
                var userEnt = ev.UserSes.AttachedEntity.Value;
                if (ev.NewBalance > ev.OldBalance)
                    _popupSystem.PopupEntity("+" + _currencyMan.Stringify(ev.NewBalance - ev.OldBalance), userEnt, userEnt, PopupType.Medium);
                else if (ev.NewBalance < ev.OldBalance)
                    _popupSystem.PopupEntity("-" + _currencyMan.Stringify(ev.OldBalance - ev.NewBalance), userEnt, userEnt, PopupType.MediumCaution);
                // I really wanted to do some fancy shit where we also display a little sprite next to the pop-up, but that gets pretty complex for such a simple interaction, so, you get this.
            }
        }
    }
}
