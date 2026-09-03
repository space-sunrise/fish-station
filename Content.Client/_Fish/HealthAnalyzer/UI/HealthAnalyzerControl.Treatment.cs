using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

#pragma warning disable IDE0130
namespace Content.Client.HealthAnalyzer.UI;
#pragma warning restore IDE0130

public sealed partial class HealthAnalyzerControl
{
    /* Формирует подсказки по лечению для данных, уже полученных анализатором. */
    private const float LowBloodLevel = 0.85f;
    private const float CryogenicMedicineMaxTemperature = 213f;
    private static readonly FixedPoint2 SevereDamageThreshold = 25;
    private static readonly ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnDamageGroup = "Burn";
    private static readonly ProtoId<DamageTypePrototype> CellularDamage = "Cellular";
    private static readonly ProtoId<DamageTypePrototype> ManglenessDamage = "Mangleness";

    private static readonly Dictionary<ProtoId<DamageGroupPrototype>, ProtoId<ReagentPrototype>> BasicTreatments = new()
    {
        ["Brute"] = "Bicaridine",
        ["Burn"] = "Kelotane",
        ["Airloss"] = "Dexalin",
        ["Toxin"] = "Dylovene",
        ["Genetic"] = "Doxarubixadone",
    };

    private static readonly Dictionary<ProtoId<DamageTypePrototype>, ProtoId<ReagentPrototype>> AdvancedTreatments = new()
    {
        ["Blunt"] = "Bruizine",
        ["Slash"] = "Lacerinol",
        ["Piercing"] = "Puncturase",
        ["Heat"] = "Pyrazine",
        ["Shock"] = "Insuzine",
        ["Cold"] = "Leporazine",
        ["Caustic"] = "Siderlac",
        ["Asphyxiation"] = "DexalinPlus",
        ["Bloodloss"] = "DexalinPlus",
        ["Poison"] = "Stellibinin",
        ["Radiation"] = "Arithrazine",
        ["Cellular"] = "Doxarubixadone",
    };

    private void DrawTreatmentAssistant(
        EntityUid target,
        MobState mobState,
        FixedPoint2 totalDamage,
        IReadOnlyDictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> damageGroups,
        IReadOnlyDictionary<ProtoId<DamageTypePrototype>, FixedPoint2> damageTypes,
        HealthAnalyzerUiState state)
    {
        TreatmentListContainer.RemoveAllChildren();

        if (float.IsNaN(state.BloodLevel))
        {
            AddTreatmentText("health-analyzer-window-treatment-unsupported");
            return;
        }

        if (mobState == MobState.Dead)
        {
            DrawDeadTreatment(target, totalDamage, damageGroups, damageTypes, state);
            return;
        }

        var recommendations = new List<TreatmentRecommendation>();
        var addedReagents = new HashSet<ProtoId<ReagentPrototype>>();

        if (mobState == MobState.Critical)
        {
            AddRecommendation(
                recommendations,
                addedReagents,
                "Epinephrine",
                Loc.GetString("health-analyzer-window-treatment-critical"));
        }

        if (state.Bleeding == true)
        {
            AddRecommendation(
                recommendations,
                addedReagents,
                "TranexamicAcid",
                Loc.GetString("health-analyzer-window-treatment-bleeding"));
        }

        if (state.BloodLevel < LowBloodLevel)
        {
            AddRecommendation(
                recommendations,
                addedReagents,
                "Saline",
                Loc.GetString(
                    "health-analyzer-window-treatment-low-blood",
                    ("amount", (state.BloodLevel * 100f).ToString("F1"))));
        }

        foreach (var (damageGroup, damageAmount) in damageGroups)
        {
            if (damageAmount <= 0 || !BasicTreatments.TryGetValue(damageGroup, out var reagent))
                continue;

            var condition = Loc.GetString(
                "health-analyzer-window-treatment-damage",
                ("damage", _prototypes.Index<DamageGroupPrototype>(damageGroup).LocalizedName),
                ("amount", damageAmount));

            if (damageAmount >= SevereDamageThreshold &&
                TryGetAdvancedTreatment(damageGroup, damageTypes, out var damageType, out var advancedReagent))
            {
                reagent = advancedReagent;
                condition = Loc.GetString(
                    "health-analyzer-window-treatment-damage",
                    ("damage", _prototypes.Index<DamageTypePrototype>(damageType).LocalizedName),
                    ("amount", damageTypes[damageType]));
            }

            AddRecommendation(recommendations, addedReagents, reagent, condition);
        }

        if (recommendations.Count == 0)
        {
            AddTreatmentText("health-analyzer-window-treatment-none");
            return;
        }

        foreach (var recommendation in recommendations)
        {
            DrawTreatmentRecommendation(recommendation, state.Reagents);
        }

        if (addedReagents.Contains("Doxarubixadone") &&
            (float.IsNaN(state.Temperature) || state.Temperature > CryogenicMedicineMaxTemperature))
        {
            AddTreatmentText(
                "health-analyzer-window-treatment-cryo-required",
                "LabelSubText",
                ("temperature", (CryogenicMedicineMaxTemperature - Atmospherics.T0C).ToString("F1")));
        }

        AddTreatmentText("health-analyzer-window-treatment-warning", "LabelSubText");
    }

    private void DrawDeadTreatment(
        EntityUid target,
        FixedPoint2 totalDamage,
        IReadOnlyDictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> damageGroups,
        IReadOnlyDictionary<ProtoId<DamageTypePrototype>, FixedPoint2> damageTypes,
        HealthAnalyzerUiState state)
    {
        if (state.Unrevivable == true)
        {
            AddTreatmentText("health-analyzer-window-treatment-dead-unrevivable");
            return;
        }

        if (_mobThresholds.TryGetDeadThreshold(target, out var deadThreshold))
        {
            var requiredHealing = FixedPoint2.Max(FixedPoint2.Zero, totalDamage - deadThreshold.Value);
            AddTreatmentText(
                "health-analyzer-window-treatment-dead-target",
                null,
                ("threshold", deadThreshold.Value),
                ("amount", requiredHealing));
        }

        if (float.IsNaN(state.Temperature) || state.Temperature > CryogenicMedicineMaxTemperature)
        {
            AddTreatmentText(
                "health-analyzer-window-treatment-cryo-required",
                "LabelSubText",
                ("temperature", (CryogenicMedicineMaxTemperature - Atmospherics.T0C).ToString("F1")));
        }

        var recommendations = new List<TreatmentRecommendation>();
        var addedReagents = new HashSet<ProtoId<ReagentPrototype>>();
        var bruteDamage = damageGroups.GetValueOrDefault(BruteDamageGroup);
        var burnDamage = damageGroups.GetValueOrDefault(BurnDamageGroup);

        if (bruteDamage > 0 && burnDamage > 0)
        {
            AddRecommendation(
                recommendations,
                addedReagents,
                "Arcryox",
                Loc.GetString("health-analyzer-window-treatment-dead-brute-burn"));
        }
        else if (bruteDamage > 0)
        {
            AddRecommendation(
                recommendations,
                addedReagents,
                "Brutedon",
                _prototypes.Index(BruteDamageGroup).LocalizedName);
        }
        else if (burnDamage > 0)
        {
            AddRecommendation(
                recommendations,
                addedReagents,
                "Aloxadone",
                _prototypes.Index(BurnDamageGroup).LocalizedName);
        }

        AddDeadDamageTreatment(recommendations, addedReagents, damageTypes, "Poison", "Antidon");
        AddDeadDamageTreatment(recommendations, addedReagents, damageTypes, "Radiation", "H-32");

        var cellularDamage = damageTypes.GetValueOrDefault(CellularDamage);
        var manglenessDamage = damageTypes.GetValueOrDefault(ManglenessDamage);
        if (cellularDamage > 0 || manglenessDamage > 0)
        {
            var damageType = cellularDamage >= manglenessDamage
                ? CellularDamage
                : ManglenessDamage;
            AddRecommendation(
                recommendations,
                addedReagents,
                "Celliminol",
                _prototypes.Index<DamageTypePrototype>(damageType).LocalizedName);
        }

        foreach (var recommendation in recommendations)
        {
            DrawTreatmentRecommendation(recommendation, state.Reagents);
        }

        AddTreatmentText("health-analyzer-window-treatment-dead-next");
        AddTreatmentText("health-analyzer-window-treatment-warning", "LabelSubText");
    }

    private void AddDeadDamageTreatment(
        List<TreatmentRecommendation> recommendations,
        HashSet<ProtoId<ReagentPrototype>> addedReagents,
        IReadOnlyDictionary<ProtoId<DamageTypePrototype>, FixedPoint2> damageTypes,
        ProtoId<DamageTypePrototype> damageType,
        ProtoId<ReagentPrototype> reagent)
    {
        if (damageTypes.GetValueOrDefault(damageType) <= 0)
            return;

        AddRecommendation(
            recommendations,
            addedReagents,
            reagent,
            _prototypes.Index<DamageTypePrototype>(damageType).LocalizedName);
    }

    private bool TryGetAdvancedTreatment(
        ProtoId<DamageGroupPrototype> damageGroup,
        IReadOnlyDictionary<ProtoId<DamageTypePrototype>, FixedPoint2> damageTypes,
        out ProtoId<DamageTypePrototype> selectedDamageType,
        out ProtoId<ReagentPrototype> reagent)
    {
        selectedDamageType = default;
        reagent = default;
        var highestDamage = FixedPoint2.Zero;
        var group = _prototypes.Index<DamageGroupPrototype>(damageGroup);

        foreach (var damageType in group.DamageTypes)
        {
            if (!AdvancedTreatments.TryGetValue(damageType, out var candidateReagent) ||
                !damageTypes.TryGetValue(damageType, out var damageAmount) ||
                damageAmount <= highestDamage)
            {
                continue;
            }

            highestDamage = damageAmount;
            selectedDamageType = damageType;
            reagent = candidateReagent;
        }

        return highestDamage > 0;
    }

    private static void AddRecommendation(
        List<TreatmentRecommendation> recommendations,
        HashSet<ProtoId<ReagentPrototype>> addedReagents,
        ProtoId<ReagentPrototype> reagent,
        string condition)
    {
        if (addedReagents.Add(reagent))
            recommendations.Add(new TreatmentRecommendation(reagent, condition));
    }

    private void DrawTreatmentRecommendation(
        TreatmentRecommendation recommendation,
        IReadOnlyList<ReagentQuantity> activeReagents)
    {
        if (!_prototypes.TryIndex<ReagentPrototype>(recommendation.Reagent, out var prototype))
            return;

        var activeAmount = FixedPoint2.Zero;
        foreach (var reagent in activeReagents)
        {
            if (reagent.Reagent.Prototype == recommendation.Reagent)
                activeAmount += reagent.Quantity;
        }

        var panel = new PanelContainer
        {
            StyleClasses = { HealthAnalyzerSheetlet.Recommendation },
        };
        var body = new BoxContainer
        {
            Margin = new Thickness(7, 5),
            Orientation = LayoutOrientation.Vertical,
        };
        var header = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
        };

        var message = new FormattedMessage();
        message.PushColor(prototype.SubstanceColor);
        message.AddText("● ");
        message.Pop();
        message.AddText(prototype.LocalizedName);

        var reagentLabel = new RichTextLabel
        {
            HorizontalExpand = true,
        };
        reagentLabel.SetMessage(message);
        header.AddChild(reagentLabel);

        if (activeAmount > 0)
        {
            header.AddChild(new Label
            {
                Text = Loc.GetString(
                    "health-analyzer-window-treatment-active-amount",
                    ("amount", activeAmount)),
                HorizontalAlignment = HAlignment.Right,
                StyleClasses =
                {
                    HealthAnalyzerSheetlet.ReagentAmount,
                    "status-good",
                },
            });
        }

        body.AddChild(header);
        var conditionLabel = new RichTextLabel
        {
            MaxWidth = 320,
            StyleClasses = { HealthAnalyzerSheetlet.DamageType },
        };
        conditionLabel.SetMessage(recommendation.Condition, HealthAnalyzerSheetlet.SecondaryText);
        body.AddChild(conditionLabel);
        panel.AddChild(body);
        TreatmentListContainer.AddChild(panel);
    }

    private void AddTreatmentText(string locId, string? styleClass = null, params (string, object)[] args)
    {
        var label = new RichTextLabel
        {
            Text = Loc.GetString(locId, args),
            MaxWidth = 330,
        };

        if (styleClass != null)
            label.StyleClasses.Add(styleClass);

        TreatmentListContainer.AddChild(label);
    }

    private readonly record struct TreatmentRecommendation(
        ProtoId<ReagentPrototype> Reagent,
        string Condition);
}
