using RolandUI.DevScope.Elements.Properties.Models;
using RolandUI.DevScope.Elements.Properties.Services;
using RolandUI.DevScope.Elements.Properties.ViewModels;
using RolandUI.DevScope.Views;

namespace RolandUI.DevScope.Views.Elements.Properties;

internal partial class ObjectPropertiesColumnView : ReactiveUserControl<ObjectPropertiesColumnViewModel>
{
    public ObjectPropertiesColumnView()
    {
        InitializeComponent();
    }
}