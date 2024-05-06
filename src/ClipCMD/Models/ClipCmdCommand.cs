namespace ClipCmd.Models;

public class ClipCmdCommand(string script)
{
    public string Script { get; set; } = script;
}