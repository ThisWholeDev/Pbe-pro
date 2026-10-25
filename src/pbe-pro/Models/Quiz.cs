namespace PbePro.Models;

public class Quiz
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>Ordered ids from the question bank. Quizzes reference questions rather than copying them.</summary>
    public List<string> QuestionIds { get; set; } = new();
}
