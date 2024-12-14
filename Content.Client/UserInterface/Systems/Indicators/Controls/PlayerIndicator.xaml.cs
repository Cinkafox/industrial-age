using Robust.Client.AutoGenerated;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Systems.Indicators.Controls;

[GenerateTypedNameReferences]
public sealed partial class PlayerIndicator : Control
{
    private Dictionary<string, IndicatorProgressBar> _bars = new Dictionary<string, IndicatorProgressBar>();
    
    public PlayerIndicator()
    {
        RobustXamlLoader.Load(this);
        UserInterfaceManager.GetUIController<IndicatorUIController>().Register(this);
    }

    public void SetEntity(EntityUid uid)
    {
        PlayerRect.SetEntity(uid);
    }

    public void ClearIndicators()
    {
        BarContainer.Children.Clear();
        _bars.Clear();
    }

    public void ChangeValue(string name, float value)
    {
        if (!_bars.TryGetValue(name, out var indicatorProgressBar))
        {
            indicatorProgressBar = AddBar(name);
        }

        if (value > indicatorProgressBar.MaxValue)
            indicatorProgressBar.MaxValue = value;
        
        indicatorProgressBar.Value = value;
    }

    private IndicatorProgressBar AddBar(string name)
    {
        var bar = new IndicatorProgressBar();
        bar.IndicatorName = name;
        _bars[name] = bar;
        BarContainer.AddChild(bar);
        return bar;
    }
}