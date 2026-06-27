using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace ClassicDiagnostics.Avalonia.Elements.Styles;

internal class BindingSetterViewModel : SetterViewModel
{
    public IBrush? Tint { get; }

    public string? ValueTypeTooltip { get; }

    public string? Path { get; }

    public BindingSetterViewModel(AvaloniaProperty property, object? value, IClipboard? clipboard) : base(property, value, clipboard)
    {
        switch (value)
        {
            case Binding binding:
            {
                Path = binding.Path;
                Tint = Brushes.CornflowerBlue;
                ValueTypeTooltip = "Reflection Binding";
                break;
            }
            case CompiledBindingExtension compiledBindingExtension:
            {
                Path = compiledBindingExtension.Path?.ToString();
                Tint = Brushes.DarkGreen;
                ValueTypeTooltip = "Compiled Binding";
                break;
            }
            case CompiledBinding compiledBinding:
            {
                Path = compiledBinding.Path?.ToString();
                Tint = Brushes.DarkGreen;
                ValueTypeTooltip = "Compiled Binding";
                break;
            }
            case TemplateBinding templateBinding:
            {
                if (templateBinding.Property is { } templateProperty)
                {
                    Path = $"{templateProperty.OwnerType.Name}.{templateProperty.Name}";
                }
                else
                {
                    Path = "Unassigned";
                }

                Tint = Brushes.OrangeRed;
                ValueTypeTooltip = "Template Binding";
                break;
            }
            case BindingBase bindingBase:
            {
                Path = bindingBase.ToString();
                Tint = Brushes.Gray;
                ValueTypeTooltip = "Other Binding";
                break;
            }
            case null:
            {
                Path = "null";
                Tint = Brushes.Gray;
                ValueTypeTooltip = "Null Binding";
                break;
            }
            default:
            {
                DevToolsDiagnostics.Report(new ArgumentException("Invalid binding type", nameof(value)), $"Invalid binding type: {value}");
                break;
            }
        }
    }

    public override void CopyValue()
    {
        if (!string.IsNullOrEmpty(Path)) CopyToClipboard(Path);
    }
}