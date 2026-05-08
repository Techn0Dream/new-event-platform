namespace TechTrek.Domain.ValueObjects;

/// <summary>
/// Value object representing a team's score.
/// Immutable - score changes produce new Score instances.
/// Ensures score cannot go negative.
/// </summary>
public sealed record Score
{
    public int Value { get; }

    public static readonly Score Zero = new(0);

    private Score(int value)
    {
        Value = value;
    }

    public static Score Of(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Score cannot be negative.");
        return new Score(value);
    }

    public Score Add(int points)
    {
        if (points < 0) throw new ArgumentOutOfRangeException(nameof(points), "Cannot add negative points.");
        return new Score(Value + points);
    }

    public static implicit operator int(Score score) => score.Value;
    public override string ToString() => Value.ToString();
}
