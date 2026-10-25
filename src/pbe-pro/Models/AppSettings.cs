namespace PbePro.Models;

public enum ThemePreference
{
    System,
    Light,
    Dark
}

public enum QuestionTextSize
{
    Small,
    Medium,
    Large,
    ExtraLarge
}

public class AppSettings
{
    /// <summary>Defaults to following the user's Windows theme.</summary>
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    /// <summary>Folder holding questions.json and quizzes.json. Null means Documents\PBE Pro.</summary>
    public string? DataFolder { get; set; }

    /// <summary>Flash the screen when a question's time runs out.</summary>
    public bool FlashOnExpire { get; set; } = true;

    /// <summary>Play a sound when a question's time runs out.</summary>
    public bool SoundOnExpire { get; set; } = true;

    /// <summary>Start each question's timer as soon as the question appears. Chosen when picking a quiz.</summary>
    public bool AutoStartTimer { get; set; } = true;

    /// <summary>Shuffle question order when starting a quiz. Chosen when picking a quiz.</summary>
    public bool ShuffleQuestions { get; set; }

    /// <summary>Don't show each question's category during a quiz. Chosen when picking a quiz.</summary>
    public bool HideCategory { get; set; }

    /// <summary>Size of the question text on the quiz screen.</summary>
    public QuestionTextSize TextSize { get; set; } = QuestionTextSize.Large;

    /// <summary>Open the app in full screen.</summary>
    public bool StartFullScreen { get; set; }
}
