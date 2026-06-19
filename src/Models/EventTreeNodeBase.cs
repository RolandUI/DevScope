using Avalonia.Collections;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Models;

internal abstract class EventTreeNodeBase : ViewModelBase
{
    internal bool _updateChildren = true;
    internal bool _updateParent = true;

    protected EventTreeNodeBase(EventTreeNodeBase? parent, string text)
    {
        Parent = parent;
        Text = text;
        IsVisible = true;
    }

    public IAvaloniaReadOnlyList<EventTreeNodeBase>? Children
    {
        get;
        protected set;
    }

    public bool IsExpanded
    {
        get;
        set => SetProperty(ref field, value);
    }

    public virtual bool? IsEnabled
    {
        get;
        set => SetProperty(ref field, value);
    } = false;

    public bool IsVisible
    {
        get;
        set => SetProperty(ref field, value);
    }

    public EventTreeNodeBase? Parent
    {
        get;
    }

    public string Text
    {
        get;
    }

    internal void UpdateChecked()
    {
        IsEnabled = GetValue();

        bool? GetValue()
        {
            if (Children == null)
                return false;

            bool? value = false;

            for (var i = 0; i < Children.Count; i++)
            {
                if (i == 0)
                {
                    value = Children[i].IsEnabled;
                    continue;
                }

                if (value != Children[i].IsEnabled)
                {
                    value = null;
                    break;
                }
            }

            return value;
        }
    }

    public virtual void Dispose()
    {
    }
}
