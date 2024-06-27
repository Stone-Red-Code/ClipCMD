using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using AvaloniaEdit;
using AvaloniaEdit.TextMate;

using ClipCmd.ViewModels;

using System;
using System.Management.Automation.Language;

using TextMateSharp.Grammars;

namespace ClipCmd.Views;

public partial class EditorWindow : Window
{
    public EditorWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        EditorViewModel editorViewModel = DataContext as EditorViewModel ?? throw new InvalidOperationException("DataContext is not EditorViewModel");

        TextEditor textEditor = this.FindControl<TextEditor>("Editor")!;

        textEditor.Text = editorViewModel.Script;

        textEditor.TextChanged += (s, e) =>
        {
            editorViewModel.Script = textEditor.Text;
            CheckScript(editorViewModel);
        };

        ThemeName editorTheme = ThemeName.DarkPlus;

        if (Avalonia.Application.Current?.ActualThemeVariant == ThemeVariant.Light)
        {
            editorTheme = ThemeName.LightPlus;
        }

        RegistryOptions registryOptions = new RegistryOptions(editorTheme);
        TextMate.Installation _textMateInstallation = textEditor.InstallTextMate(registryOptions);

        _textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".ps1").Id));

        CheckScript(editorViewModel);
    }

    private void CheckScript(EditorViewModel editorViewModel)
    {
        _ = Parser.ParseInput(editorViewModel.Script, out _, out ParseError[] parseErrors);

        editorViewModel.Messages.Clear();

        if (parseErrors.Length > 0)
        {
            foreach (ParseError parseError in parseErrors)
            {
                editorViewModel.Messages.Add(new($"Error: {parseError.Message} at line {parseError.Extent.StartLineNumber} and column {parseError.Extent.StartColumnNumber}", Brushes.Red));
            }
        }
        else
        {
            editorViewModel.Messages.Add(new("No errors", Brushes.Green));
        }
    }
}