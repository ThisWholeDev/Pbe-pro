namespace PbePro.Models;

public class Question
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public string Answer { get; set; } = "";

    /// <summary>Point value. Must be 1 or more; the time limit is derived from it.</summary>
    public int Points { get; set; } = 1;

    public string? Category { get; set; }
}
