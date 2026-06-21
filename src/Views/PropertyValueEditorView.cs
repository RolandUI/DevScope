using System.Globalization;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.VisualTree;
using ClassicDiagnostics.Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Properties;
using ClassicDiagnostics.Avalonia.ViewModels;
using Path = Avalonia.Controls.Shapes.Path;

namespace ClassicDiagnostics.Avalonia.Views;

internal class PropertyValueEditorView : ReactiveUserControl<PropertyViewModel>
{
    private PropertyViewModel? Property => ViewModel;

    private readonly static Geometry ImageIcon = Geometry.Parse(
        "M12.25 6C8.79822 6 6 8.79822 6 12.25V35.75C6 37.1059 6.43174 38.3609 7.16525 39.3851L21.5252 25.0251C22.8921 23.6583 25.1081 23.6583 26.475 25.0251L40.8348 39.385C41.5683 38.3608 42 37.1058 42 35.75V12.25C42 8.79822 39.2018 6 35.75 6H12.25ZM34.5 17.5C34.5 19.7091 32.7091 21.5 30.5 21.5C28.2909 21.5 26.5 19.7091 26.5 17.5C26.5 15.2909 28.2909 13.5 30.5 13.5C32.7091 13.5 34.5 15.2909 34.5 17.5ZM39.0024 41.0881L24.7072 26.7929C24.3167 26.4024 23.6835 26.4024 23.293 26.7929L8.99769 41.0882C9.94516 41.6667 11.0587 42 12.25 42H35.75C36.9414 42 38.0549 41.6666 39.0024 41.0881Z");

    private readonly static Geometry GeometryIcon = Geometry.Parse(
        "M23.25 15.5H30.8529C29.8865 8.99258 24.2763 4 17.5 4C10.0442 4 4 10.0442 4 17.5C4 24.2763 8.99258 29.8865 15.5 30.8529V23.25C15.5 18.9698 18.9698 15.5 23.25 15.5ZM23.25 18C20.3505 18 18 20.3505 18 23.25V38.75C18 41.6495 20.3505 44 23.25 44H38.75C41.6495 44 44 41.6495 44 38.75V23.25C44 20.3505 41.6495 18 38.75 18H23.25Z");

    private readonly static ColorToBrushConverter Color2Brush = new();

    private readonly CompositeDisposable _cleanup = new();

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        Content = UpdateControl();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        _cleanup.Clear();
    }

    private Control? UpdateControl()
    {
        _cleanup.Clear();

        if (Property is not { } property)
        {
            return null;
        }

        var descriptor = PropertyEditorFactory.Default.Create(property);
        var propertyType = descriptor.PropertyType;

        switch (descriptor.Kind)
        {
            case PropertyEditorKind.Boolean:
                return CreateControl<CheckBox>(ToggleButton.IsCheckedProperty);

            case PropertyEditorKind.Numeric:
                return CreateControl<NumericUpDown>(
                    NumericUpDown.ValueProperty,
                    new ValueToDecimalConverter(),
                    n =>
                    {
                        n.Increment = 1;
                        n.NumberFormat = new NumberFormatInfo { NumberDecimalDigits = 0 };
                        n.ParsingNumberStyle = NumberStyles.Integer;
                    },
                    NumericUpDown.IsReadOnlyProperty);

            case PropertyEditorKind.Color:
                return CreateColorEditor();

            case PropertyEditorKind.Brush:
                return CreateControl<BrushEditor>(BrushEditor.BrushProperty);

            case PropertyEditorKind.Image:
            case PropertyEditorKind.Geometry:
                return CreatePreviewEditor(descriptor.Kind);

            case PropertyEditorKind.Enum:
                return CreateControl<ComboBox>(
                    SelectingItemsControl.SelectedItemProperty,
                    init: c =>
                    {
                        c.ItemsSource = Enum.GetValues(propertyType);
                    });

            case PropertyEditorKind.FlagsEnum:
                return CreateFlagsEnumEditor(descriptor);

            case PropertyEditorKind.Text:
            case PropertyEditorKind.ReadOnlyText:
                return CreateTextEditor(descriptor);

            case PropertyEditorKind.ComplexObject:
                return CreateNavigateButton(descriptor);

            default:
                return null;
        }

        Control CreateColorEditor()
        {
            var el = new Ellipse { Width = 12, Height = 12, VerticalAlignment = VerticalAlignment.Center };

            el.Bind(
                    Shape.FillProperty,
                    new Binding(nameof(Property.Value)) { Source = Property, Converter = Color2Brush })
                .DisposeWith(_cleanup);

            var tbl = new TextBlock { VerticalAlignment = VerticalAlignment.Center };

            tbl.Bind(
                    TextBlock.TextProperty,
                    new Binding(nameof(Property.Value)) { Source = Property })
                .DisposeWith(_cleanup);

            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Children = { el, tbl },
                Background = Brushes.Transparent,
                Cursor = new Cursor(StandardCursorType.Hand),
                IsEnabled = !Property.IsReadonly,
            };

            var cv = new ColorView
            {
                HexInputAlphaPosition = AlphaComponentPosition.Leading, // Always match XAML
            };

            cv.Bind(
                    ColorView.ColorProperty,
                    new Binding(nameof(Property.Value))
                    {
                        Source = Property,
                        Converter = Color2Brush,
                        Mode = BindingMode.TwoWay,
                    })
                .DisposeWith(_cleanup);

            FlyoutBase.SetAttachedFlyout(sp, new Flyout { Content = cv });

            sp.PointerPressed += (_, _) => FlyoutBase.ShowAttachedFlyout(sp);

            return sp;
        }

        Control CreatePreviewEditor(PropertyEditorKind kind)
        {
            var isImage = kind == PropertyEditorKind.Image;
            var valueObservable = Property.GetObservable(x => x.Value);
            var tbl = new TextBlock { VerticalAlignment = VerticalAlignment.Center };

            tbl.Bind(
                    TextBlock.TextProperty,
                    valueObservable.Select(value => value switch
                    {
                        IImage img => $"{img.Size.Width} x {img.Size.Height}",
                        Geometry geom => $"{geom.Bounds.Width} x {geom.Bounds.Height}",
                        _ => "(null)",
                    }))
                .DisposeWith(_cleanup);

            var sp = new StackPanel
            {
                Background = Brushes.Transparent,
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Children =
                {
                    new Path
                    {
                        Data = isImage ? ImageIcon : GeometryIcon,
                        Fill = Brushes.Gray,
                        Width = 12,
                        Height = 12,
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    tbl,
                },
            };

            if (isImage)
            {
                var previewImage = new Image { Stretch = Stretch.Uniform, Width = 300, Height = 300 };

                previewImage
                    .Bind(Image.SourceProperty, valueObservable)
                    .DisposeWith(_cleanup);

                ToolTip.SetTip(sp, previewImage);
            }
            else
            {
                var previewShape = new Path
                {
                    Stretch = Stretch.Uniform,
                    Fill = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };

                previewShape
                    .Bind(Path.DataProperty, valueObservable)
                    .DisposeWith(_cleanup);

                ToolTip.SetTip(sp, new Border { Child = previewShape, Width = 300, Height = 300 });
            }

            return sp;
        }

        CommitTextBox CreateTextEditor(PropertyEditorDescriptor textDescriptor)
        {
            var tb = CreateControl<CommitTextBox>(
                CommitTextBox.CommittedTextProperty,
                new TextToValueConverter(),
                t =>
                {
                    t.PlaceholderText = "(null)";
                },
                TextBox.IsReadOnlyProperty);

            tb.IsReadOnly |= !textDescriptor.CanEdit;

            if (!tb.IsReadOnly)
            {
                tb.GetObservable(TextBox.TextProperty).Subscribe(t =>
                {
                    try
                    {
                        if (t != null)
                        {
                            PropertyStringConversion.FromString(t, propertyType);
                        }

                        DataValidationErrors.ClearErrors(tb);
                    }
                    catch (Exception ex)
                    {
                        DataValidationErrors.SetError(tb, ex.GetBaseException());
                    }
                }).DisposeWith(_cleanup);
            }

            return tb;
        }

        Control CreateNavigateButton(PropertyEditorDescriptor valueDescriptor)
        {
            var text = new TextBlock
            {
                TextTrimming = TextTrimming.PrefixCharacterEllipsis,
            };

            text.Bind(
                    TextBlock.TextProperty,
                    new Binding(nameof(Property.Value))
                    {
                        Source = Property,
                        Converter = new TextToValueConverter(),
                    })
                .DisposeWith(_cleanup);

            var button = new Button
            {
                Content = text,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsEnabled = valueDescriptor.CanNavigate,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };

            button.Bind(
                    ToolTip.TipProperty,
                    new Binding(nameof(Property.Value))
                    {
                        Source = Property,
                        Converter = new TextToValueConverter(),
                    })
                .DisposeWith(_cleanup);

            button.Click += (_, _) =>
            {
                var details = this.FindAncestorOfType<ControlPropertiesView>()?.ViewModel ??
                    this.FindAncestorOfType<ControlDetailsView>()?.ViewModel;

                if (details is null)
                {
                    return;
                }

                details.SelectedProperty = property;
                details.NavigateToSelectedProperty();
            };

            return button;
        }

        Control CreateFlagsEnumEditor(PropertyEditorDescriptor flagsDescriptor)
        {
            var model = new FlagsEnumEditorModel(flagsDescriptor.PropertyType);
            var checkBoxes = new List<CheckBox>();
            var isRefreshing = false;
            var button = new Button
            {
                IsEnabled = flagsDescriptor.CanEdit,
            };

            button.Bind(
                    ContentControl.ContentProperty,
                    new Binding(nameof(Property.Value)) { Source = Property })
                .DisposeWith(_cleanup);

            var optionsPanel = new StackPanel
            {
                Margin = new Thickness(4),
                Spacing = 2,
            };

            foreach (var option in model.Options)
            {
                var checkBox = new CheckBox
                {
                    Content = option.Name,
                    IsChecked = model.IsSelected(property.Value, option),
                };

                checkBox.PropertyChanged += OptionChanged;
                checkBoxes.Add(checkBox);
                optionsPanel.Children.Add(checkBox);

                void OptionChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
                {
                    if (isRefreshing || e.Property != ToggleButton.IsCheckedProperty)
                    {
                        return;
                    }

                    property.Value = model.Toggle(property.Value, option, checkBox.IsChecked == true);
                    RefreshOptions();
                }
            }

            var clearButton = new Button
            {
                Content = "Clear",
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            clearButton.Click += (_, _) =>
            {
                property.Value = model.Clear();
                RefreshOptions();
            };

            optionsPanel.Children.Add(clearButton);
            FlyoutBase.SetAttachedFlyout(button, new Flyout { Content = optionsPanel });

            button.Click += (_, _) => FlyoutBase.ShowAttachedFlyout(button);

            return button;

            void RefreshOptions()
            {
                isRefreshing = true;

                for (var i = 0; i < model.Options.Count; i++)
                {
                    checkBoxes[i].IsChecked = model.IsSelected(property.Value, model.Options[i]);
                }

                isRefreshing = false;
            }
        }

        TControl CreateControl<TControl>(
            AvaloniaProperty valueProperty,
            IValueConverter? converter = null,
            Action<TControl>? init = null,
            AvaloniaProperty? readonlyProperty = null)
            where TControl : Control, new()
        {
            var control = new TControl();
            var bindingMode = Property.IsReadonly ? BindingMode.OneWay : BindingMode.TwoWay;

            init?.Invoke(control);

            control.Bind(
                valueProperty,
                new Binding(nameof(Property.Value))
                {
                    Source = Property,
                    Converter = converter ?? new ValueConverter(),
                    ConverterParameter = propertyType,
                    Mode = bindingMode,
                }).DisposeWith(_cleanup);

            if (readonlyProperty != null)
            {
                control[readonlyProperty] = Property.IsReadonly;
            }
            else
            {
                control.IsEnabled = !Property.IsReadonly;
            }

            return control;
        }
    }

    private class ValueConverter : IValueConverter
    {
        object? IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }

        object? IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            //Note: targetType provided by Converter is simply "object"
            return ConvertBack(value, (Type)parameter!, parameter, culture);
        }

        protected virtual object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value;
        }

        protected virtual object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            return value;
        }
    }

    private sealed class ValueToDecimalConverter : ValueConverter
    {
        protected override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return PropertyNumericConversion.ToDecimal(value);
        }

        protected override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return PropertyNumericConversion.FromDecimal(value, targetType);
        }
    }

    private sealed class TextToValueConverter : ValueConverter
    {
        protected override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is null ? null : PropertyStringConversion.ToString(value);
        }

        protected override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string s)
                return null;

            try
            {
                return PropertyStringConversion.FromString(s, targetType);
            }
            catch
            {
                return BindingOperations.DoNothing;
            }
        }
    }
}