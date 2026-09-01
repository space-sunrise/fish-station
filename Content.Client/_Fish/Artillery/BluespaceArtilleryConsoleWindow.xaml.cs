using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared._Fish.Artillery;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using Content.Shared.Explosion;

namespace Content.Client._Fish.Artillery;

public sealed class BluespaceArtilleryConsoleWindow : DefaultWindow
{
    private readonly LineEdit _coordinateX;
    private readonly LineEdit _coordinateY;
    private readonly Button _applyCoordinates;
    private readonly Label _currentCoords;
    private readonly Label _statusLabel;
    private readonly Control _scannerControl;
    private readonly TextureRect _crosshair;
    private readonly OptionButton _explosionType;
    private readonly LineEdit _intensity;
    private readonly LineEdit _slope;
    private readonly LineEdit _maxIntensity;
    private readonly CheckBox _previewToggle;
    private readonly Button _fireButton;

    private ArtilleryVector2 _targetCoords;
    private const float ScannerMaxOffset = 16384f;
    private const float TileSize = 32f;

    private readonly List<string> _explosionTypes = new();

    public event Action? OnFire;
    public event Action<ArtilleryVector2>? OnCoordsChanged;
    public event Action<string, float, float, float>? OnParamsChanged;
    public event Action<bool>? OnPreviewToggled;

    public BluespaceArtilleryConsoleWindow()
    {
        RobustXamlLoader.Load(this);

        _coordinateX = this.FindControl<LineEdit>("CoordinateX");
        _coordinateY = this.FindControl<LineEdit>("CoordinateY");
        _applyCoordinates = this.FindControl<Button>("ApplyCoordinates");
        _currentCoords = this.FindControl<Label>("CurrentCoords");
        _statusLabel = this.FindControl<Label>("StatusLabel");
        _scannerControl = this.FindControl<Control>("ScannerControl");
        _crosshair = this.FindControl<TextureRect>("Crosshair");
        _crosshair.MinSize = new Vector2(32, 32);
        _explosionType = this.FindControl<OptionButton>("ExplosionType");
        _intensity = this.FindControl<LineEdit>("Intensity");
        _slope = this.FindControl<LineEdit>("Slope");
        _maxIntensity = this.FindControl<LineEdit>("MaxIntensity");
        _previewToggle = this.FindControl<CheckBox>("PreviewToggle");
        _fireButton = this.FindControl<Button>("FireButton");

        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        foreach (var proto in prototypeManager.EnumeratePrototypes<ExplosionPrototype>())
        {
            _explosionTypes.Add(proto.ID);
        }
        _explosionTypes.Sort();

        foreach (var type in _explosionTypes)
            _explosionType.AddItem(type);

        var resCache = IoCManager.Resolve<IResourceCache>();
        if (resCache.TryGetResource<RSIResource>(new ResPath("/Textures/Interface/Misc/crosshair_pointers.rsi"), out var rsi))
        {
            var state = rsi.RSI["gun_sight"];
            if (state != null)
            {
                _crosshair.Texture = state.Frame0;
                _crosshair.Visible = true;
            }
        }

        _applyCoordinates.OnPressed += _ => ApplyManualCoordinates();
        _fireButton.OnPressed += _ => OnFire?.Invoke();
        _previewToggle.OnToggled += args => OnPreviewToggled?.Invoke(args.Pressed);

        _explosionType.OnItemSelected += _ => SendParams();
        _intensity.OnTextEntered += _ => SendParams();
        _slope.OnTextEntered += _ => SendParams();
        _maxIntensity.OnTextEntered += _ => SendParams();

        _targetCoords = ArtilleryVector2.Zero;
        UpdateCoordFields();

        _scannerControl.OnResized += UpdateCrosshairPosition;
        UpdateCrosshairPosition();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var rect = _scannerControl.GlobalRect;
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        handle.DrawRect(rect, new Color(20, 20, 30, 255));

        float halfSize = MathF.Min(rect.Width, rect.Height) / 2f;
        float metersPerPixel = ScannerMaxOffset / halfSize;

        var center = rect.Center;

        int tileSizeMeters = (int)TileSize;
        float leftM = _targetCoords.X - halfSize * metersPerPixel;
        float rightM = _targetCoords.X + halfSize * metersPerPixel;
        float topM = _targetCoords.Y + halfSize * metersPerPixel;
        float bottomM = _targetCoords.Y - halfSize * metersPerPixel;

        int startX = (int)MathF.Floor(leftM / tileSizeMeters) * tileSizeMeters;
        for (int x = startX; x <= rightM; x += tileSizeMeters)
        {
            float screenX = center.X + (x - _targetCoords.X) / metersPerPixel;
            handle.DrawLine(new Vector2(screenX, rect.Top), new Vector2(screenX, rect.Bottom), new Color(60, 60, 80, 100));
        }

        int startY = (int)MathF.Floor(bottomM / tileSizeMeters) * tileSizeMeters;
        for (int y = startY; y <= topM; y += tileSizeMeters)
        {
            float screenY = center.Y - (y - _targetCoords.Y) / metersPerPixel;
            handle.DrawLine(new Vector2(rect.Left, screenY), new Vector2(rect.Right, screenY), new Color(60, 60, 80, 100));
        }
    }

    private void ApplyManualCoordinates()
    {
        if (float.TryParse(_coordinateX.Text, out var x) &&
            float.TryParse(_coordinateY.Text, out var y))
        {
            _targetCoords = new ArtilleryVector2(x, y);
            ClampCoordinates();
            UpdateCoordFields();
            OnCoordsChanged?.Invoke(_targetCoords);
        }
    }

    private void ClampCoordinates()
    {
        _targetCoords.X = Math.Clamp(_targetCoords.X, -ScannerMaxOffset, ScannerMaxOffset);
        _targetCoords.Y = Math.Clamp(_targetCoords.Y, -ScannerMaxOffset, ScannerMaxOffset);
    }

    private void UpdateCoordFields()
    {
        _coordinateX.Text = _targetCoords.X.ToString("F1");
        _coordinateY.Text = _targetCoords.Y.ToString("F1");
        _currentCoords.Text = $"X: {_targetCoords.X:F1}, Y: {_targetCoords.Y:F1}";
    }

    private void UpdateCrosshairPosition()
    {
        var containerSize = _scannerControl.Size;
        if (containerSize.X <= 0 || containerSize.Y <= 0)
            return;

        var center = containerSize / 2f;
        var crossSize = _crosshair.Size;
        LayoutContainer.SetPosition(_crosshair, center - crossSize / 2f);
    }

    private void SendParams()
    {
        if (_explosionType.SelectedId < 0 || _explosionType.SelectedId >= _explosionTypes.Count)
            return;

        if (float.TryParse(_intensity.Text, out var intensity) &&
            float.TryParse(_slope.Text, out var slope) &&
            float.TryParse(_maxIntensity.Text, out var max))
        {
            var type = _explosionTypes[_explosionType.SelectedId];
            OnParamsChanged?.Invoke(type, intensity, slope, max);
        }
    }

    public void UpdateState(BluespaceArtilleryConsoleBoundUserInterfaceState state)
    {
        _targetCoords = state.TargetCoordinates;
        UpdateCoordFields();
        UpdateCrosshairPosition();

        var typeIndex = _explosionTypes.IndexOf(state.ExplosionType);
        if (_explosionTypes.Count > 0)
            _explosionType.SelectId(typeIndex >= 0 ? typeIndex : 0);

        _intensity.Text = state.TotalIntensity.ToString();
        _slope.Text = state.Slope.ToString();
        _maxIntensity.Text = state.MaxIntensity.ToString();
        _previewToggle.Pressed = state.PreviewEnabled;

        if (!state.IsLinked)
            _statusLabel.Text = Loc.GetString("bluespace-artillery-status-no-link");
        else if (state.IsCharging)
            _statusLabel.Text = Loc.GetString("bluespace-artillery-status-charging");
        else if (state.IsOnCooldown)
            _statusLabel.Text = Loc.GetString("bluespace-artillery-status-cooldown");
        else
            _statusLabel.Text = Loc.GetString("bluespace-artillery-status-ready");

        _fireButton.Disabled = !state.IsLinked || state.IsCharging || state.IsOnCooldown;
    }
}