namespace ClassicDiagnostics.Avalonia;

public static class VisualTreeDebug
{
    public static string PrintVisualTree(Visual visual)
    {
        var result = StringBuilderCache.Acquire();
        PrintVisualTree(visual, result, 0);
        return StringBuilderCache.GetStringAndRelease(result);
    }

    private static void PrintVisualTree(Visual visual, StringBuilder builder, int indent)
    {
        var control = visual as Control;

        builder.Append(Indent(indent - 1));

        if (indent > 0)
        {
            builder.Append(" +- ");
        }

        builder.Append(visual.GetType().Name);

        if (control != null)
        {
            builder.Append(' ');
            builder.AppendLine(control.Classes.ToString());

            foreach (var property in AvaloniaPrivateApi.Current.GetRegisteredProperties(control))
            {
                var value = AvaloniaPrivateApi.Current.GetDiagnosticValue(control, property);
                if (value.Priority == BindingPriority.Unset) continue;

                builder.Append(Indent(indent));
                builder.Append(" |  ");
                builder.Append(value.Property.Name);
                builder.Append(" = ");
                builder.Append(value.Value ?? "(null)");
                builder.Append(" [");
                builder.Append(value.Priority);
                builder.AppendLine("]");
            }
        }
        else
        {
            builder.AppendLine();
        }

        foreach (var child in visual.VisualChildren)
        {
            PrintVisualTree(child, builder, indent + 1);
        }
    }

    private static string Indent(int indent)
    {
        return new string(' ', Math.Max(indent, 0) * 4);
    }
}
