using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml.MarkupExtensions;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Styles;

internal class ValueFrameViewModel : ViewModelBase
{
    public bool IsActive
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsVisible
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string? Description { get; }

    public List<SetterViewModel> Setters { get; }

    private readonly IValueFrameDiagnostic _valueFrame;

    public ValueFrameViewModel(StyledElement styledElement, IValueFrameDiagnostic valueFrame, IClipboard? clipboard)
    {
        _valueFrame = valueFrame;
        IsVisible = true;

        var source = SourceToString(_valueFrame.Source);
        Description = (_valueFrame.Type, source) switch
        {
            (IValueFrameDiagnostic.FrameType.Local, _) => "Local Values " + source,
            (IValueFrameDiagnostic.FrameType.Template, _) => "Template " + source,
            (IValueFrameDiagnostic.FrameType.Theme, _) => "Theme " + source,
            (_, { Length: > 0 }) => source,
            _ => _valueFrame.Priority.ToString(),
        };

        Setters = [];

        foreach (var (setterProperty, setterValue) in valueFrame.Values)
        {
            var resourceInfo = GetResourceInfo(setterValue);

            SetterViewModel setterViewModel;
            if (resourceInfo.HasValue)
            {
                var resourceKey = resourceInfo.Value.resourceKey;
                var resourceValue = styledElement.FindResource(resourceKey);
                setterViewModel = new ResourceSetterViewModel(
                    setterProperty,
                    resourceKey,
                    resourceValue,
                    resourceInfo.Value.isDynamic,
                    clipboard);
            }
            else
            {
                if (setterValue is BindingBase)
                {
                    setterViewModel = new BindingSetterViewModel(setterProperty, setterValue, clipboard);
                }
                else
                {
                    setterViewModel = new SetterViewModel(setterProperty, setterValue, clipboard);
                }
            }

            Setters.Add(setterViewModel);
        }

        Update();
    }

    public void Update()
    {
        IsActive = _valueFrame.IsActive;
    }

    private static (object resourceKey, bool isDynamic)? GetResourceInfo(object? value)
    {
        return value switch
        {
            StaticResourceExtension { ResourceKey: not null } staticResource => (staticResource.ResourceKey, false),
            DynamicResourceExtension { ResourceKey: not null } dynamicResource => (dynamicResource.ResourceKey, true),
            _ => null
        };
    }

    private static string? SourceToString(object? source)
    {
        switch (source)
        {
            case Style style:
            {
                StyleBase? currentStyle = style;
                var selectors = new Stack<string>();

                while (currentStyle is not null)
                {
                    switch (currentStyle)
                    {
                        case Style { Selector: { } selector }:
                            selectors.Push(selector.ToString());
                            break;
                        case ControlTheme theme:
                            selectors.Push("Theme " + theme.TargetType?.Name);
                            break;
                    }

                    currentStyle = currentStyle.Parent as StyleBase;
                }

                return string.Concat(selectors).Replace("^", "");
            }
            case ControlTheme controlTheme:
                return controlTheme.TargetType?.Name;
            case StyledElement styledElement:
                return styledElement.StyleKey.Name;
            default:
                return null;
        }
    }
}