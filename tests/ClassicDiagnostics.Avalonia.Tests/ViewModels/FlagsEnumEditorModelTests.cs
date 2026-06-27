using ClassicDiagnostics.Avalonia.Properties;
using ClassicDiagnostics.Avalonia.Elements;
using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;
using ClassicDiagnostics.Avalonia.Elements.Trees;
using ClassicDiagnostics.Avalonia.Rooting;
using ClassicDiagnostics.Avalonia.Shell;

namespace ClassicDiagnostics.Avalonia.Tests.ViewModels;

internal sealed class FlagsEnumEditorModelTests
{
    [Test]
    public void ModelCreatesSingleBitOptionsOnly()
    {
        var model = new FlagsEnumEditorModel(typeof(FileAccessFlags));

        Assert.That(model.Options.Select(option => option.Name), Is.EqualTo(new[] { "Read", "Write", "Execute" }));
        Assert.That(model.Options.Select(option => option.RawValue), Is.EqualTo(new ulong[] { 1, 2, 4 }));
    }

    [Test]
    public void ModelReadsSelectionFromCurrentValue()
    {
        var model = new FlagsEnumEditorModel(typeof(FileAccessFlags));
        var value = FileAccessFlags.Read | FileAccessFlags.Execute;

        Assert.That(model.IsSelected(value, model.Options.Single(option => option.Name == "Read")), Is.True);
        Assert.That(model.IsSelected(value, model.Options.Single(option => option.Name == "Write")), Is.False);
        Assert.That(model.IsSelected(value, model.Options.Single(option => option.Name == "Execute")), Is.True);
    }

    [Test]
    public void ModelTogglesOptionsAndClearsValue()
    {
        var model = new FlagsEnumEditorModel(typeof(FileAccessFlags));
        var read = model.Options.Single(option => option.Name == "Read");
        var write = model.Options.Single(option => option.Name == "Write");

        var value = model.Toggle(FileAccessFlags.None, read, true);
        value = model.Toggle(value, write, true);

        Assert.That(value, Is.EqualTo(FileAccessFlags.Read | FileAccessFlags.Write));

        value = model.Toggle(value, read, false);

        Assert.That(value, Is.EqualTo(FileAccessFlags.Write));
        Assert.That(model.Clear(), Is.EqualTo(FileAccessFlags.None));
    }

    [Flags]
    private enum FileAccessFlags
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        ReadWrite = Read | Write,
        All = -1,
        AlsoRead = Read,
    }
}
