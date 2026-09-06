using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;

// Общий namespace связывает эту часть с исходной системой анализатора.
namespace Content.Server.Medical;

public sealed partial class HealthAnalyzerSystem
{
    /* Состав крови для интерфейса Fish без изменения растворов пациента. */

    /// <summary>
    /// Copies substances outside the patient's reference blood composition into the scan result.
    /// Matching by prototype keeps natural blood variants out of the medication list.
    /// </summary>
    private static void CollectForeignReagents(Solution blood, Solution referenceBlood, List<ReagentQuantity> reagents)
    {
        foreach (var reagent in blood.Contents)
        {
            var isBloodReagent = false;

            foreach (var bloodReagent in referenceBlood.Contents)
            {
                if (bloodReagent.Reagent.Prototype != reagent.Reagent.Prototype)
                    continue;

                isBloodReagent = true;
                break;
            }

            if (!isBloodReagent)
                reagents.Add(new ReagentQuantity(reagent.Reagent.Prototype, reagent.Quantity));
        }
    }
}
