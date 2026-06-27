namespace ClassicDiagnostics.Avalonia.Views.Controls;

//TODO: UpdateSourceTrigger & Binding.ValidationRules could help removing the need for this control.
internal sealed class CommitTextBox : TextBox
{
    /// <summary>
    ///     Defines the <see cref="CommittedText" /> property.
    /// </summary>
    public readonly static DirectProperty<CommitTextBox, string?> CommittedTextProperty =
        AvaloniaProperty.RegisterDirect<CommitTextBox, string?>(
            nameof(CommittedText),
            o => o.CommittedText,
            (o, v) => o.CommittedText = v);

    protected override Type StyleKeyOverride => typeof(TextBox);

    public string? CommittedText
    {
        get;
        set => SetAndRaise(CommittedTextProperty, ref field, value);
    }

    public event EventHandler<CommitTextBoxCommitEventArgs>? CommitRequested;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CommittedTextProperty)
        {
            Text = CommittedText;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        switch (e.Key)
        {
            case Key.Enter:
                TryCommit();
                e.Handled = true;
                break;
            case Key.Escape:
                Cancel();
                e.Handled = true;
                break;
        }
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);

        TryCommit();
    }

    private void Cancel()
    {
        Text = CommittedText;
        DataValidationErrors.ClearErrors(this);
    }

    private void TryCommit()
    {
        var args = new CommitTextBoxCommitEventArgs(Text);
        CommitRequested?.Invoke(this, args);

        if (!args.Cancel && !DataValidationErrors.GetHasErrors(this))
        {
            CommittedText = Text;
        }
        else
        {
            Text = CommittedText;
            DataValidationErrors.ClearErrors(this);
        }
    }
}

internal sealed class CommitTextBoxCommitEventArgs(string? text) : EventArgs
{
    public string? Text { get; } = text;

    public bool Cancel { get; set; }
}