// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Shared.MisandryBox;
using Content.Shared.Mobs.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.MisandryBox.Appender;

public sealed class AccountAppenderSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    FrozenDictionary<string, AccountAppendPrototype> _protoIds = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<MobStateComponent, PlayerDetachedEvent>(OnPlayerDetached);

        _protoIds = _protoMan.GetInstances<AccountAppendPrototype>();
    }

    private void OnPlayerAttached(Entity<MobStateComponent> ent, ref PlayerAttachedEvent args)
    {
        var sesh = args.Player;
        if (!TryGetComps(sesh, out var comps))
            return;

        foreach (var comp in comps)
        {
            AddComp(args.Entity, comp, overwrite: true);
        }
    }

    private void OnPlayerDetached(Entity<MobStateComponent> ent, ref PlayerDetachedEvent args)
    {
        var sesh = args.Player;
        if (!TryGetComps(sesh, out var comps))
            return;

        foreach (var comp in comps)
        {
            RemComp(args.Entity, comp);
        }
    }

    private bool TryGetComps(ICommonSession sesh, [NotNullWhen(true)] out List<IComponent>? comps)
    {
        comps = [];

        if (!_protoIds.TryGetValue(sesh.Name.ToLowerInvariant(), out var proto))
        {
            if (!TryGuidFallback(sesh, out proto))
                return false;
        }

        foreach (var comp in proto.Components)
        {
            comps.Add((Component) _compFactory.GetComponent(comp, true));
        }

        return comps.Count > 0;
    }

    private bool TryGuidFallback(ICommonSession sesh,
        [NotNullWhen(true)] out AccountAppendPrototype? prototype)
    {
        prototype = null;
        var userid = sesh.UserId;

        foreach (var proto in _protoIds.Values)
        {
            if (proto.Userid == Guid.Empty || proto.Userid != userid)
                continue;

            prototype = proto;
            return true;
        }

        return false;
    }
}