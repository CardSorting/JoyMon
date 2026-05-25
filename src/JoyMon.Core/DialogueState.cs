namespace JoyMon.Core;

public class DialogueState
{
    public string Speaker { get; private set; } = string.Empty;
    public IReadOnlyList<string> Lines { get; private set; } = Array.Empty<string>();
    public int CurrentLineIndex { get; private set; } = -1;

    public bool IsActive => CurrentLineIndex >= 0 && CurrentLineIndex < Lines.Count;

    public string CurrentLine => IsActive ? Lines[CurrentLineIndex] : string.Empty;

    public void Start(string speaker, IReadOnlyList<string> lines)
    {
        Speaker = speaker;
        Lines = lines;
        CurrentLineIndex = 0;
    }

    public void Advance()
    {
        if (IsActive)
        {
            CurrentLineIndex++;
        }
    }

    public void Close()
    {
        CurrentLineIndex = -1;
        Speaker = string.Empty;
        Lines = Array.Empty<string>();
    }
}
