using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using Structa.Core.Editor;
using Structa.Core.Messaging;

namespace Structa.UI.Views;

/// <summary>
/// Traduz mouse/teclado do <c>InputSurface</c> (overlay transparente) para a navegação da câmera e as
/// ferramentas expostas por <see cref="RenderViewport"/>: botão do meio arrasta = órbita, Shift + botão
/// do meio arrasta = pan, roda do mouse = zoom, Home = resetar vista. Botão esquerdo = ação da
/// ferramenta ativa (selecionar, com Shift somando à seleção atual; colocar ponto da ferramenta Linha;
/// ou agarrar a seleção/uma face e começar o arrasto de Empurrar/Puxar, Mover, Rotacionar ou Escalar,
/// confirmado ao soltar o botão). Ctrl ao começar um arrasto de Mover duplica a seleção em vez de
/// movê-la no lugar. Esc = cancelar operação pendente / limpar seleção. Atalhos: L = Linha, P =
/// Empurrar/Puxar, M = Mover, R = Rotacionar, S = Escalar. Com uma ferramenta de arrasto em andamento,
/// dígitos/ponto/sinal digitam um valor exato (distância, ângulo ou fator), Backspace apaga e Enter
/// confirma (equivalente a soltar o botão).
/// </summary>
public partial class ViewportView : UserControl
{
    // Alguns dispositivos/drivers reportam o mesmo clique físico como dois PointerPressed
    // praticamente simultâneos (mesma posição, poucos ms de diferença). Sem esse filtro, um clique
    // aditivo (Shift) alternaria a seleção duas vezes e pareceria não fazer nada.
    private const double DuplicateClickWindowMs = 200;
    private const double DuplicateClickDistance = 3;

    private readonly IEventAggregator _eventAggregator = App.Services.GetRequiredService<IEventAggregator>();

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
        InputSurface.PointerExited += OnInputSurfacePointerExited;
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
            RenderSurface.HandlePrimaryClick(
                point.Position,
                e.KeyModifiers.HasFlag(KeyModifiers.Shift),
                e.KeyModifiers.HasFlag(KeyModifiers.Control));
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
        var position = e.GetCurrentPoint(InputSurface).Position;

        // Sempre repassado, mesmo sem botão pressionado: as ferramentas de desenho/transformação
        // precisam da posição atual do cursor a cada frame para desenhar o preview.
        RenderSurface.UpdatePointer(position);

        if (!_isOrbiting && !_isPanning)
        {
            return;
        }

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

    private void OnInputSurfacePointerExited(object? sender, PointerEventArgs e) => RenderSurface.UpdatePointer(null);

    private void OnInputSurfacePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            RenderSurface.CommitActiveTool();
        }

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
            RenderSurface.CancelActiveTool();
            e.Handled = true;
        }
        else if (TryGetToolShortcut(e.Key, out var tool))
        {
            _eventAggregator.Publish(new EditorToolChangedEvent(tool));
            e.Handled = true;
        }
        else if (RenderSurface.IsTransformToolActive && TryHandleToolValueKey(e.Key))
        {
            e.Handled = true;
        }
    }

    private static bool TryGetToolShortcut(Key key, out EditorTool tool)
    {
        switch (key)
        {
            case Key.L:
                tool = EditorTool.Line;
                return true;
            case Key.P:
                tool = EditorTool.PushPull;
                return true;
            case Key.M:
                tool = EditorTool.Move;
                return true;
            case Key.R:
                tool = EditorTool.Rotate;
                return true;
            case Key.S:
                tool = EditorTool.Scale;
                return true;
            default:
                tool = default;
                return false;
        }
    }

    /// <summary>Dígitos, ponto decimal, sinal de menos, Backspace e Enter controlam o valor exato
    /// (distância, ângulo ou fator, conforme a ferramenta ativa) enquanto o arrasto está em andamento.
    /// Retorna false para qualquer outra tecla.</summary>
    private bool TryHandleToolValueKey(Key key)
    {
        if (TryGetDigit(key, out var digit))
        {
            RenderSurface.AppendActiveToolCharacter(digit);
            return true;
        }

        switch (key)
        {
            case Key.OemPeriod or Key.Decimal:
                RenderSurface.AppendActiveToolCharacter('.');
                return true;
            case Key.OemMinus or Key.Subtract:
                RenderSurface.AppendActiveToolCharacter('-');
                return true;
            case Key.Back:
                RenderSurface.RemoveActiveToolCharacter();
                return true;
            case Key.Enter:
                RenderSurface.CommitActiveTool();
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetDigit(Key key, out char digit)
    {
        if (key >= Key.D0 && key <= Key.D9)
        {
            digit = (char)('0' + (key - Key.D0));
            return true;
        }

        if (key >= Key.NumPad0 && key <= Key.NumPad9)
        {
            digit = (char)('0' + (key - Key.NumPad0));
            return true;
        }

        digit = default;
        return false;
    }
}
