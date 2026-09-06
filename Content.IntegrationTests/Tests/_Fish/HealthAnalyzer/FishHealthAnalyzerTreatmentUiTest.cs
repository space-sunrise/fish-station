using System.Collections.Generic;
using System.Linq;
using Content.Client._Fish.HealthAnalyzer.UI;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.MedicalScanner;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Fish.HealthAnalyzer;

[TestFixture]
[NonParallelizable]
public sealed class FishHealthAnalyzerTreatmentUiTest
{
    [Test]
    public async Task ConflictingActiveRazoriumReactantsWarnWithoutRecommendations()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;
        var localization = client.ResolveDependency<ILocalizationManager>();
        FishHealthAnalyzerControl control = default!;
        EntityUid patient = default;

        await client.WaitPost(() =>
        {
            patient = client.EntMan.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            control = new FishHealthAnalyzerControl();
            control.Populate(new HealthAnalyzerUiState(
                client.EntMan.GetNetEntity(patient),
                310f,
                1f,
                true,
                false,
                false,
                100f,
                100f,
                [
                    new ReagentQuantity("Bicaridine", FixedPoint2.New(5)),
                    new ReagentQuantity("Lacerinol", FixedPoint2.New(5)),
                ]));
        });

        await client.WaitAssertion(() =>
        {
            var expected = localization.GetString("health-analyzer-window-treatment-razorium-warning");
            Assert.That(Descendants(control).OfType<RichTextLabel>().Any(label => label.Text == expected), Is.True);
        });

        await client.WaitPost(() => client.EntMan.DeleteEntity(patient));
        await pair.CleanReturnAsync();
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (var child in root.Children)
        {
            yield return child;

            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }
}
