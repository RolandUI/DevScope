namespace RolandUI.DevScope.Views.Controls;

internal class FilterTextBox : TextBox
{
    public readonly static StyledProperty<bool> UseRegexFilterProperty =
        AvaloniaProperty.Register<FilterTextBox, bool>(
            nameof(UseRegexFilter),
            defaultBindingMode: BindingMode.TwoWay);

    public readonly static StyledProperty<bool> UseCaseSensitiveFilterProperty =
        AvaloniaProperty.Register<FilterTextBox, bool>(
            nameof(UseCaseSensitiveFilter),
            defaultBindingMode: BindingMode.TwoWay);

    public readonly static StyledProperty<bool> UseWholeWordFilterProperty =
        AvaloniaProperty.Register<FilterTextBox, bool>(
            nameof(UseWholeWordFilter),
            defaultBindingMode: BindingMode.TwoWay);

    public FilterTextBox()
    {
        Classes.Add("filter-text-box");
    }

    public bool UseRegexFilter
    {
        get => GetValue(UseRegexFilterProperty);
        set => SetValue(UseRegexFilterProperty, value);
    }

    public bool UseCaseSensitiveFilter
    {
        get => GetValue(UseCaseSensitiveFilterProperty);
        set => SetValue(UseCaseSensitiveFilterProperty, value);
    }

    public bool UseWholeWordFilter
    {
        get => GetValue(UseWholeWordFilterProperty);
        set => SetValue(UseWholeWordFilterProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(TextBox);
}