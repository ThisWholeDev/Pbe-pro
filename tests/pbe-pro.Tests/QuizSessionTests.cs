using PbePro.Models;
using PbePro.Services;

namespace PbePro.Tests;

public class QuizSessionTests
{
    private static Question Q(string id, int points) =>
        new() { Id = id, Text = $"Text {id}", Answer = $"Answer {id}", Points = points };

    private static QuizSession ThreeQuestions() => new(new[] { Q("a", 1), Q("b", 2), Q("c", 1) });

    [Fact]
    public void StartsOnTheFirstQuestion()
    {
        var session = ThreeQuestions();

        Assert.Equal("a", session.Current!.Id);
        Assert.Equal(1, session.CurrentNumber);
        Assert.Equal(3, session.Total);
        Assert.Equal(0, session.AnsweredCount);
        Assert.False(session.IsFinished);
        Assert.False(session.CanUndo);
    }

    [Fact]
    public void Mark_AdvancesAndCounts()
    {
        var session = ThreeQuestions();

        session.Mark(true);
        session.Mark(false);

        Assert.Equal("c", session.Current!.Id);
        Assert.Equal(3, session.CurrentNumber);
        Assert.Equal(2, session.AnsweredCount);
        Assert.Equal(1, session.CorrectCount);
    }

    [Fact]
    public void Points_AreTrackedSeparatelyFromCounts()
    {
        var session = ThreeQuestions();

        session.Mark(false); // a: 1 pt, missed
        session.Mark(true);  // b: 2 pts, correct

        Assert.Equal(2, session.PointsEarned);
        Assert.Equal(3, session.PointsAnswered);
        Assert.Equal(4, session.PointsTotal);
    }

    [Fact]
    public void Finishing_ClearsCurrent_AndKeepsNumberOnTheLastQuestion()
    {
        var session = ThreeQuestions();

        session.Mark(true);
        session.Mark(true);
        session.Mark(true);

        Assert.True(session.IsFinished);
        Assert.Null(session.Current);
        Assert.Equal(3, session.CurrentNumber);
    }

    [Fact]
    public void Mark_AfterFinishing_Throws()
    {
        var session = new QuizSession(new[] { Q("a", 1) });
        session.Mark(true);

        Assert.Throws<InvalidOperationException>(() => session.Mark(true));
    }

    [Fact]
    public void Undo_RestoresThePreviousQuestion_AndRemovesItsMark()
    {
        var session = ThreeQuestions();
        session.Mark(true);
        session.Mark(false);

        var restored = session.Undo();

        Assert.Equal("b", restored.Id);
        Assert.Equal("b", session.Current!.Id);
        Assert.Equal(1, session.AnsweredCount);
        Assert.Equal(1, session.CorrectCount);
        Assert.Equal(1, session.PointsEarned);
    }

    [Fact]
    public void Undo_AfterFinishing_ReopensTheLastQuestion()
    {
        var session = new QuizSession(new[] { Q("a", 1), Q("b", 1) });
        session.Mark(true);
        session.Mark(true);

        session.Undo();

        Assert.False(session.IsFinished);
        Assert.Equal("b", session.Current!.Id);
    }

    [Fact]
    public void Undo_WithNothingToUndo_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ThreeQuestions().Undo());
    }

    [Fact]
    public void Missed_ListsIncorrectQuestionsInOrder()
    {
        var session = ThreeQuestions();
        session.Mark(false);
        session.Mark(true);
        session.Mark(false);

        Assert.Equal(new[] { "a", "c" }, session.Missed.Select(q => q.Id));
    }

    [Fact]
    public void Missed_IgnoresQuestionsNotYetAnswered()
    {
        var session = ThreeQuestions();
        session.Mark(false);

        Assert.Equal("a", Assert.Single(session.Missed).Id);
    }

    [Fact]
    public void EmptySession_IsFinishedImmediately()
    {
        var session = new QuizSession(Array.Empty<Question>());

        Assert.True(session.IsFinished);
        Assert.Null(session.Current);
        Assert.Equal(0, session.Total);
    }
}
