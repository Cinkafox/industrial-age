using Robust.Client.AutoGenerated;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Systems.Indicators.Controls;

[GenerateTypedNameReferences]
public sealed partial class PlayerIndicator : Control
{
    public PlayerIndicator()
    {
        RobustXamlLoader.Load(this);
        UserInterfaceManager.GetUIController<IndicatorUIController>().Register(this);
    }

    public void SetEntity(EntityUid uid)
    {
        PlayerRect.SetEntity(uid);
    }
}