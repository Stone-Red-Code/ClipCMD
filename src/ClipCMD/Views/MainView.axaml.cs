using Avalonia.Controls;

using AvaloniaEdit;
using AvaloniaEdit.TextMate;

using System.Diagnostics;
using System.Management.Automation.Language;

using TextMateSharp.Grammars;

namespace ClipCmd.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        return;
        //First of all you need to have a reference for your TextEditor for it to be used inside AvaloniaEdit.TextMate project.
        TextEditor? textEditor = this.FindControl<TextEditor>("TextEditor");

        textEditor.TextChanged += (s, e) =>
        {
            _ = Parser.ParseInput(textEditor.Text, out _, out ParseError[] parseErrors);

            if (parseErrors.Length > 0)
            {
                foreach (ParseError parseError in parseErrors)
                {
                    Debug.WriteLine(parseError.Message + " at line " + parseError.Extent.StartLineNumber + " and column " + parseError.Extent.StartColumnNumber);
                }
            }
        };
        RegistryOptions registryOptions = new RegistryOptions(ThemeName.DarkPlus);

        //Initial setup of TextMate.
        TextMate.Installation _textMateInstallation = textEditor.InstallTextMate(registryOptions);

        //Here we are getting the language by the extension and right after that we are initializing grammar with this language.
        //And that's all 😀, you are ready to use AvaloniaEdit with syntax highlighting!
        _textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".ps1").Id));
    }
}