using TechTrek.Domain.Exceptions;
using TechTrek.Domain.Primitives;

namespace TechTrek.Domain.Entities;

/// <summary>
/// Question entity - a coding problem teams must solve.
/// Contains the problem statement, expected I/O, point value.
/// Linked to a Riddle via the EventStage (not directly).
/// 
/// Maps exactly to frontend mockDatabase questions:
/// { id, title, description, inputs, outputs, points, riddleId }
/// 
/// NOT an aggregate root. Questions are managed independently but
/// are read through EventRepository for game state queries.
/// </summary>
public sealed class Question : AuditableEntity
{
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string InputFormat { get; private set; } = default!;
    public string OutputFormat { get; private set; } = default!;
    public string? SampleInput { get; private set; }
    public string? SampleOutput { get; private set; }
    public string? Constraints { get; private set; }
    public int Points { get; private set; }
    public int TimeLimitSeconds { get; private set; } = 120;
    public int MemoryLimitMb { get; private set; } = 256;
    public bool IsActive { get; private set; } = true;
    public Guid CreatedByEventId { get; private set; }

    private Question() { }

    public static Question Create(
        string title,
        string description,
        string inputFormat,
        string outputFormat,
        int points,
        Guid eventId,
        string? sampleInput = null,
        string? sampleOutput = null,
        string? constraints = null,
        int timeLimitSeconds = 120,
        int memoryLimitMb = 256)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Question title cannot be empty.");
        if (points <= 0)
            throw new DomainException("Points must be positive.");

        return new Question
        {
            Title = title.Trim(),
            Description = description.Trim(),
            InputFormat = inputFormat.Trim(),
            OutputFormat = outputFormat.Trim(),
            Points = points,
            CreatedByEventId = eventId,
            SampleInput = sampleInput,
            SampleOutput = sampleOutput,
            Constraints = constraints,
            TimeLimitSeconds = timeLimitSeconds,
            MemoryLimitMb = memoryLimitMb
        };
    }

    public void Update(string title, string description, string inputFormat, string outputFormat, int points)
    {
        Title = title.Trim();
        Description = description.Trim();
        InputFormat = inputFormat.Trim();
        OutputFormat = outputFormat.Trim();
        Points = points;
        Touch();
    }
}

/// <summary>
/// Riddle entity - the physical location clue.
/// Teams must find the location, say the answer to the VOLUNTEER stationed there.
/// Volunteer sees: location + answer.
/// Participant sees: riddle question only.
/// 
/// Maps to frontend: { id, question, answer, location }
/// Answer is ENCRYPTED in database (volunteers should not be able to pre-read via DB access).
/// </summary>
public sealed class Riddle : AuditableEntity
{
    public string QuestionText { get; private set; } = default!;
    public string AnswerEncrypted { get; private set; } = default!; // AES-256
    public string Location { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private Riddle() { }

    public static Riddle Create(string questionText, string answerEncrypted, string location)
    {
        if (string.IsNullOrWhiteSpace(questionText))
            throw new DomainException("Riddle question cannot be empty.");
        if (string.IsNullOrWhiteSpace(answerEncrypted))
            throw new DomainException("Riddle answer cannot be empty.");
        if (string.IsNullOrWhiteSpace(location))
            throw new DomainException("Riddle location cannot be empty.");

        return new Riddle
        {
            QuestionText = questionText.Trim(),
            AnswerEncrypted = answerEncrypted,
            Location = location.Trim()
        };
    }
}
