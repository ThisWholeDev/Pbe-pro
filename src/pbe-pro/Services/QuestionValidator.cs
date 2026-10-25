using PbePro.Models;

namespace PbePro.Services;

public static class QuestionValidator
{
    /// <summary>Returns a list of problems; empty means the question is valid. Ids are checked elsewhere.</summary>
    public static List<string> Validate(Question question)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(question.Text))
            problems.Add("Question text is required.");

        if (string.IsNullOrWhiteSpace(question.Answer))
            problems.Add("Answer is required.");

        if (question.Points < 1)
            problems.Add("Points must be 1 or more.");

        return problems;
    }
}
