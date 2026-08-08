using Content.Server.Administration.Logs;
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Systems;
using Content.Server.Stack;
using Content.Shared._Fish.Research;
using Content.Shared._Fish.Research.Components;
using Content.Shared.Audio;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Research.Components;
using Content.Shared.Stacks;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Fish.Research;

/// <summary>
/// Серверная логика destructive analyzer: валидация → анализ → уничтожение → ModifyServerResearchPoints.
/// Повторная награда исключена уничтожением экземпляра (или 1 единицы стека).
/// </summary>
public sealed class DestructiveAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DestructiveAnalyzerComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, DestructiveAnalyzerAnalyzeMessage>(OnAnalyze);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, DestructiveAnalyzerEjectMessage>(OnEject);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, ResearchRegistrationChangedEvent>(OnRegistrationChanged);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DestructiveAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.AnalysisFinishTime is not { } finishTime)
                continue;

            if (_timing.CurTime < finishTime)
                continue;

            FinishAnalysis(uid, component);
        }
    }

    private void OnUiOpened(Entity<DestructiveAnalyzerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnBeforeUiOpen(Entity<DestructiveAnalyzerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnContainerModified<T>(EntityUid uid, DestructiveAnalyzerComponent component, T args)
    {
        UpdateUserInterface((uid, component));
    }

    private void OnPointsChanged(Entity<DestructiveAnalyzerComponent> ent, ref ResearchServerPointsChangedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnRegistrationChanged(Entity<DestructiveAnalyzerComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnPowerChanged(Entity<DestructiveAnalyzerComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered && ent.Comp.AnalysisFinishTime != null)
            CancelAnalysis(ent, eject: false);

        UpdateUserInterface(ent);
    }

    private void OnExamined(Entity<DestructiveAnalyzerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.AnalysisFinishTime != null)
        {
            args.PushMarkup(Loc.GetString("destructive-analyzer-examine-busy"));
            return;
        }

        var item = _slots.GetItemOrNull(ent.Owner, ent.Comp.SlotId);
        if (item != null)
            args.PushMarkup(Loc.GetString("destructive-analyzer-examine-loaded"));
        else
            args.PushMarkup(Loc.GetString("destructive-analyzer-examine-empty"));
    }

    private void OnShutdown(Entity<DestructiveAnalyzerComponent> ent, ref ComponentShutdown args)
    {
        SetAnalyzingAmbience(ent.Owner, false);
    }

    private void OnEject(Entity<DestructiveAnalyzerComponent> ent, ref DestructiveAnalyzerEjectMessage args)
    {
        if (ent.Comp.AnalysisFinishTime != null)
        {
            _popup.PopupEntity(Loc.GetString("destructive-analyzer-busy"), ent, args.Actor);
            return;
        }

        if (!_slots.TryGetSlot(ent.Owner, ent.Comp.SlotId, out var slot))
            return;

        _slots.TryEjectToHands(ent.Owner, slot, args.Actor);
        UpdateUserInterface(ent);
    }

    private void OnAnalyze(Entity<DestructiveAnalyzerComponent> ent, ref DestructiveAnalyzerAnalyzeMessage args)
    {
        var user = args.Actor;

        // Защита от гонок: один активный анализ на машину.
        if (ent.Comp.AnalysisFinishTime != null)
        {
            _popup.PopupEntity(Loc.GetString("destructive-analyzer-busy"), ent, user);
            return;
        }

        if (!this.IsPowered(ent.Owner, EntityManager))
        {
            _popup.PopupEntity(Loc.GetString("destructive-analyzer-no-power"), ent, user);
            return;
        }

        if (!_research.TryGetClientServer(ent.Owner, out _, out _))
        {
            _popup.PopupEntity(Loc.GetString("destructive-analyzer-no-server"), ent, user);
            _audio.PlayPvs(ent.Comp.FailSound, ent);
            return;
        }

        var item = _slots.GetItemOrNull(ent.Owner, ent.Comp.SlotId);
        if (item == null)
        {
            _popup.PopupEntity(Loc.GetString("destructive-analyzer-empty"), ent, user);
            _audio.PlayPvs(ent.Comp.FailSound, ent);
            return;
        }

        if (TerminatingOrDeleted(item.Value))
        {
            _popup.PopupEntity(Loc.GetString("destructive-analyzer-invalid"), ent, user);
            _audio.PlayPvs(ent.Comp.FailSound, ent);
            return;
        }

        if (!TryComp<ResearchAnalyzableComponent>(item.Value, out var analyzable) || analyzable.Points <= 0)
        {
            _popup.PopupEntity(Loc.GetString("destructive-analyzer-invalid"), ent, user);
            _audio.PlayPvs(ent.Comp.FailSound, ent);
            return;
        }

        // Фиксируем награду и EntityUid до уничтожения — клиент не передаёт Points.
        ent.Comp.AnalyzingItem = item.Value;
        ent.Comp.PendingPoints = analyzable.Points;
        ent.Comp.AnalysisFinishTime = _timing.CurTime + ent.Comp.AnalysisDuration;
        Dirty(ent.Owner, ent.Comp);

        _slots.SetLock(ent.Owner, ent.Comp.SlotId, true);
        SetAnalyzingAmbience(ent.Owner, true);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):player} started destructive analysis of {ToPrettyString(item.Value)} (+{analyzable.Points} pending) at {ToPrettyString(ent.Owner)}");
        UpdateUserInterface(ent);
    }

    private void FinishAnalysis(EntityUid uid, DestructiveAnalyzerComponent component)
    {
        var analyzingItem = component.AnalyzingItem;
        var pendingPoints = component.PendingPoints;

        // Сначала снимаем busy-состояние, чтобы сбой не оставлял машину залоченной навсегда.
        ClearAnalysisState(uid, component);

        if (analyzingItem is not { } item || TerminatingOrDeleted(item))
        {
            UpdateUserInterface((uid, component));
            return;
        }

        // Предмет мог быть удалён / подменён во время обработки.
        var slotItem = _slots.GetItemOrNull(uid, component.SlotId);
        if (slotItem != item)
        {
            UpdateUserInterface((uid, component));
            return;
        }

        if (!TryComp<ResearchAnalyzableComponent>(item, out var analyzable) || analyzable.Points <= 0)
        {
            UpdateUserInterface((uid, component));
            return;
        }

        // Пересчитываем на сервере: берём минимум из зафиксированного и актуального значения.
        var reward = Math.Min(pendingPoints, analyzable.Points);
        if (reward <= 0)
        {
            UpdateUserInterface((uid, component));
            return;
        }

        if (!_research.TryGetClientServer(uid, out var server, out var serverComp))
        {
            UpdateUserInterface((uid, component));
            return;
        }

        if (!this.IsPowered(uid, EntityManager))
        {
            UpdateUserInterface((uid, component));
            return;
        }

        var itemString = ToPrettyString(item);

        // Уничтожение ДО начисления: при ошибке удаления очки не выдаём.
        if (!TryDestroyAnalyzedItem(uid, component, item))
        {
            _popup.PopupEntity(Loc.GetString("destructive-analyzer-destroy-failed"), uid);
            UpdateUserInterface((uid, component));
            return;
        }

        _research.ModifyServerResearchPoints(server.Value, reward, serverComp);
        _audio.PlayPvs(component.AnalyzeSound, uid);
        _popup.PopupEntity(Loc.GetString("destructive-analyzer-success", ("points", reward)), uid, PopupType.Medium);
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"Destructive analyzer {ToPrettyString(uid)} destroyed {itemString} for {reward} research points on server {ToPrettyString(server.Value)}");
        UpdateUserInterface((uid, component));
    }

    /// <summary>
    /// Уничтожает один экземпляр: для стеков потребляет 1 единицу, иначе QueueDel.
    /// </summary>
    private bool TryDestroyAnalyzedItem(EntityUid analyzer, DestructiveAnalyzerComponent component, EntityUid item)
    {
        if (TryComp<StackComponent>(item, out var stack) && stack.Count > 1)
        {
            // Одна единица стека = одно исследование; остаток остаётся в слоте.
            if (!_stack.TryUse((item, stack), 1))
                return false;

            return true;
        }

        // Полное уничтожение единственного экземпляра / последнего из стека.
        QueueDel(item);
        return true;
    }

    private void CancelAnalysis(Entity<DestructiveAnalyzerComponent> ent, bool eject)
    {
        ClearAnalysisState(ent.Owner, ent.Comp);

        if (eject && _slots.TryGetSlot(ent.Owner, ent.Comp.SlotId, out var slot))
            _slots.TryEject(ent.Owner, slot, null, out _);
    }

    private void ClearAnalysisState(EntityUid uid, DestructiveAnalyzerComponent component)
    {
        component.AnalysisFinishTime = null;
        component.AnalyzingItem = null;
        component.PendingPoints = 0;
        Dirty(uid, component);
        _slots.SetLock(uid, component.SlotId, false);
        SetAnalyzingAmbience(uid, false);
    }

    private void SetAnalyzingAmbience(EntityUid uid, bool enabled)
    {
        if (HasComp<AmbientSoundComponent>(uid))
            _ambientSound.SetAmbience(uid, enabled);
    }

    private void UpdateUserInterface(Entity<DestructiveAnalyzerComponent> ent)
    {
        if (!_ui.HasUi(ent.Owner, DestructiveAnalyzerUiKey.Key))
            return;

        var connected = _research.TryGetClientServer(ent.Owner, out _, out var serverComp);
        var points = connected && serverComp != null ? serverComp.Points : 0;
        var item = _slots.GetItemOrNull(ent.Owner, ent.Comp.SlotId);
        var isAnalyzing = ent.Comp.AnalysisFinishTime != null;

        string? itemName = null;
        var researchValue = 0;
        var canAnalyze = false;

        if (item != null && !TerminatingOrDeleted(item.Value))
        {
            itemName = FormattedMessage.EscapeText(Identity.Name(item.Value, EntityManager));
            if (TryComp<ResearchAnalyzableComponent>(item.Value, out var analyzable))
            {
                researchValue = analyzable.Points;
                canAnalyze = !isAnalyzing
                    && researchValue > 0
                    && connected
                    && this.IsPowered(ent.Owner, EntityManager);
            }
        }

        var state = new DestructiveAnalyzerBoundUserInterfaceState(
            points,
            connected,
            item != null,
            itemName,
            researchValue,
            canAnalyze,
            isAnalyzing);

        _ui.SetUiState(ent.Owner, DestructiveAnalyzerUiKey.Key, state);
    }
}
