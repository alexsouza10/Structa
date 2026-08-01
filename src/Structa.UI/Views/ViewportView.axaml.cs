using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Structa.UI.Views;

/// <summary>
/// Traduz mouse/teclado do <c>InputSurface</c> (overlay transparente) para a navegação da câmera e o
/// picking expostos por <see cref="RenderViewport"/>: botão do meio arrasta = órbita, Shift + botão do
/// meio arrasta = pan, roda do mouse = zoom, Home = resetar vista. Botão esquerdo = selecionar (Shift
/// soma à seleção atual), Esc = limpar seleção.
/// </summary>
public partial class ViewportView : UserControl
{
    // Alguns dispositivos/drivers reportam o mesmo clique físico como dois PointerPressed
    // praticamente simultâneos (mesma posição, poucos ms de diferença). Sem esse filtro, um clique
    // aditivo (Shift) alternaria a seleção duas vezes e pareceria não fazer nada.
    private const double DuplicateClickWindowMs = 200;
    private const double DuplicateClickDistance = 3;

    private bool _isOrbiting;
    private bool _isPanning;
    private Point _lastPointerPosition;
    private DateTime _lastPickTime;
    private Point _lastPickPosition;

    public ViewportView()
    {
        InitializeComponent();

        InputSurface.PointerPressed += OnInputSurfacePointerPressed;
        InputSurface.PointerMoved += OnInputSurfacePointerMoved;
        InputSurface.PointerReleased += OnInputSurfacePointerReleased;
        InputSurface.PointerWheelChanged += OnInputSurfacePointerWheelChanged;
        InputSurface.KeyDown += OnInputSurfaceKeyDown;
    }

    private void OnInputSurfacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(InputSurface);

        if (point.Properties.IsLeftButtonPressed)
        {
            e.Handled = true;

            var now = DateTime.UtcNow;
            var dx = point.Position.X - _lastPickPosition.X;
            var dy = point.Position.Y - _lastPickPosition.Y;
            var isDuplicate = (now - _lastPickTime).TotalMilliseconds < DuplicateClickWindowMs
                && (dx * dx) + (dy * dy) < DuplicateClickDistance * DuplicateClickDistance;

            _lastPickTime = now;
            _lastPickPosition = point.Position;

            if (isDuplicate)
            {
                return;
            }

            InputSurface.Focus();
            RenderSurface.Pick(point.Position, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
            return;
        }

        if (!point.Properties.IsMiddleButtonPressed)
        {
            return;
        }

        InputSurface.Focus();
        _isPanning = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        _isOrbiting = !_isPanning;
        _lastPointerPosition = point.Position;
        e.Pointer.Capture(InputSurface);
        e.Handled = true;
    }

    private void OnInputSurfacePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isOrbiting && !_isPanning)
        {
            return;
        }

        var position = e.GetCurrentPoint(InputSurface).Position;
        var delta = position - _lastPointerPosition;
        _lastPointerPosition = position;

        if (_isOrbiting)
        {
            RenderSurface.Orbit(delta.X, delta.Y);
        }
        else
        {
            RenderSurface.Pan(delta.X, delta.Y);
        }

        e.Handled = true;
    }

    private void OnInputSurfacePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isOrbiting && !_isPanning)
        {
            return;
        }

        _isOrbiting = false;
        _isPanning = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnInputSurfacePointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        RenderSurface.Zoom(e.Delta.Y);
        e.Handled = true;
    }

    private void OnInputSurfaceKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Home)
        {
            RenderSurface.ResetView();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            RenderSurface.ClearSelection();
            e.Handled = true;
        }
    }
}
