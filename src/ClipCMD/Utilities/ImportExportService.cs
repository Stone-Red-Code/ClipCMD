using Avalonia.Controls;
using Avalonia.Platform.Storage;

using ClipCmd.Models;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClipCmd.Utilities;

public class ImportExportService(Window window, ClipCmdCommandHandler clipCmdCommandHandler)
{
    private readonly TopLevel topLevel = window;

    public async Task Import()
    {
        // Start async operation to open the dialog.
        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import ClipCMD commands",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("ccmd")
            {
                Patterns = ["*.ccmd"]
            }]
        });

        if (files.Count < 1)
        {
            return;
        }

        await using Stream stream = await files[0].OpenReadAsync();
        using StreamReader streamReader = new StreamReader(stream);

        string json = await streamReader.ReadToEndAsync();

        clipCmdCommandHandler.ClearCommands();

        Dictionary<string, ClipCmdCommand>? commands = JsonSerializer.Deserialize<Dictionary<string, ClipCmdCommand>>(json);

        if (commands is null)
        {
            return;
        }

        foreach (KeyValuePair<string, ClipCmdCommand> command in commands)
        {
            clipCmdCommandHandler.Commands.Add(command.Key, command.Value);
        }
    }

    public async Task Export()
    {
        // Start async operation to open the dialog.
        IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export ClipCMD commands",
            SuggestedFileName = "export.ccmd",
            FileTypeChoices = [new FilePickerFileType("ccmd")
            {
                Patterns = ["*.ccmd"]
            }]
        });

        if (file is null)
        {
            return;
        }

        string json = JsonSerializer.Serialize(clipCmdCommandHandler.Commands);

        await using Stream stream = await file.OpenWriteAsync();
        using StreamWriter streamWriter = new StreamWriter(stream);
        await streamWriter.WriteAsync(json);
    }
}