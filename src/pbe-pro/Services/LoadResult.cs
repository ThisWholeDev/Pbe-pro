namespace PbePro.Services;

/// <summary>Items that loaded successfully, plus a readable message for each entry that didn't.</summary>
public class LoadResult<T>
{
    public List<T> Items { get; } = new();
    public List<string> Errors { get; } = new();
    public bool HasErrors => Errors.Count > 0;
}
