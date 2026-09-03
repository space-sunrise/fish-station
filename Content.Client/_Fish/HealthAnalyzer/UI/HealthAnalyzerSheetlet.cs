using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Palette;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.HealthAnalyzer.UI;

[CommonSheetlet]
public sealed class HealthAnalyzerSheetlet : Sheetlet<PalettedStylesheet>
{
    public const string Card = "HealthAnalyzerCard";
    public const string Window = "HealthAnalyzerWindow";
    public const string WindowHeader = "HealthAnalyzerWindowHeader";
    public const string WindowTitle = "HealthAnalyzerWindowTitle";
    public const string HeaderCard = "HealthAnalyzerHeaderCard";
    public const string PortraitFrame = "HealthAnalyzerPortraitFrame";
    public const string MetricCard = "HealthAnalyzerMetricCard";
    public const string DamageGroup = "HealthAnalyzerDamageGroup";
    public const string DamageType = "HealthAnalyzerDamageType";
    public const string TreatmentCard = "HealthAnalyzerTreatmentCard";
    public const string Recommendation = "HealthAnalyzerRecommendation";
    public const string AlertCard = "HealthAnalyzerAlertCard";
    public const string EmptyCard = "HealthAnalyzerEmptyCard";
    public const string SectionTitle = "HealthAnalyzerSectionTitle";
    public const string PatientName = "HealthAnalyzerPatientName";
    public const string MetricKey = "HealthAnalyzerMetricKey";
    public const string MetricValue = "HealthAnalyzerMetricValue";
    public const string Status = "HealthAnalyzerStatus";
    public const string ScanMode = "HealthAnalyzerScanMode";
    public const string ReagentAmount = "HealthAnalyzerReagentAmount";

    public static Color SecondaryText => MedicalPalette.TextDark;

    private static readonly ColorPalette MedicalPalette = ColorPalette.FromHexBase(
        "#55B8C4",
        lightnessShift: 0.055f,
        chromaShift: 0.008f,
        background: Color.FromHex("#18272E"),
        text: Color.FromHex("#A4E8EC"));

    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var card = CreatePanel(MedicalPalette.BackgroundDark, MedicalPalette.Element, new Thickness(2, 0, 0, 0));
        var header = CreatePanel(MedicalPalette.Background, MedicalPalette.Base, new Thickness(0, 0, 0, 2));
        var portrait = CreatePanel(MedicalPalette.BackgroundDark, MedicalPalette.Element, new Thickness(1));
        var metric = CreatePanel(MedicalPalette.BackgroundDark, MedicalPalette.BackgroundLight, new Thickness(1));
        var damageGroup = CreatePanel(MedicalPalette.Background, MedicalPalette.BackgroundLight, new Thickness(0, 0, 0, 1));
        var treatment = CreatePanel(MedicalPalette.BackgroundDark, MedicalPalette.Base, new Thickness(3, 0, 0, 0));
        var recommendation = CreatePanel(MedicalPalette.Background, MedicalPalette.Element, new Thickness(1, 0, 0, 0));
        var alert = CreatePanel(Palettes.Red.BackgroundDark, Palettes.Amber.Base, new Thickness(3, 0, 0, 0));
        var empty = CreatePanel(MedicalPalette.BackgroundDark, MedicalPalette.BackgroundLight, new Thickness(1));
        var windowHeader = CreatePanel(MedicalPalette.BackgroundLight, MedicalPalette.Base, new Thickness(0, 0, 0, 2));

        return
        [
            E<FancyWindow>()
                .Class(Window)
                .ParentOf(E<PanelContainer>().Class(StyleClass.BackgroundPanel))
                .Modulate(MedicalPalette.Background),
            E<PanelContainer>().Class(WindowHeader).Panel(windowHeader),
            E<PanelContainer>().Class(Card).Panel(card),
            E<PanelContainer>().Class(HeaderCard).Panel(header),
            E<PanelContainer>().Class(PortraitFrame).Panel(portrait),
            E<PanelContainer>().Class(MetricCard).Panel(metric),
            E<PanelContainer>().Class(DamageGroup).Panel(damageGroup),
            E<PanelContainer>().Class(TreatmentCard).Panel(treatment),
            E<PanelContainer>().Class(Recommendation).Panel(recommendation),
            E<PanelContainer>().Class(AlertCard).Panel(alert),
            E<PanelContainer>().Class(EmptyCard).Panel(empty),

            E<Label>()
                .Class(WindowTitle)
                .Font(sheet.BaseFont.GetFont(14, FontKind.Bold))
                .FontColor(MedicalPalette.Text),
            E<Label>()
                .Class(SectionTitle)
                .Font(sheet.BaseFont.GetFont(13, FontKind.Bold))
                .FontColor(MedicalPalette.Text),
            E<Label>()
                .Class(PatientName)
                .Font(sheet.BaseFont.GetFont(16, FontKind.Bold)),
            E<Label>()
                .Class(MetricKey)
                .Font(sheet.BaseFont.GetFont(10))
                .FontColor(MedicalPalette.TextDark),
            E<Label>()
                .Class(MetricValue)
                .Font(sheet.BaseFont.GetFont(14, FontKind.Bold)),
            E<Label>()
                .Class(Status)
                .Font(sheet.BaseFont.GetFont(12, FontKind.Bold)),
            E<Label>()
                .Class(ScanMode)
                .Font(sheet.BaseFont.GetFont(10, FontKind.Bold)),
            E<Label>()
                .Class(DamageType)
                .Font(sheet.BaseFont.GetFont(11))
                .FontColor(MedicalPalette.TextDark),
            E<RichTextLabel>()
                .Class(DamageType)
                .Font(sheet.BaseFont.GetFont(11)),
            E<Label>()
                .Class(ReagentAmount)
                .Font(sheet.BaseFont.GetFont(11, FontKind.Bold))
                .FontColor(MedicalPalette.Text),
        ];
    }

    private static StyleBoxFlat CreatePanel(Color background, Color border, Thickness thickness)
    {
        return new StyleBoxFlat
        {
            BackgroundColor = background,
            BorderColor = border,
            BorderThickness = thickness,
        };
    }
}
