using Xunit;
using FluentAssertions;
using TechTrek.Domain.Aggregates;

namespace TechTrek.Tests;

public class TeamTests
{
    [Fact]
    public void Team_Create_ShouldInitializeCorrectly()
    {
        // Arrange
        var teamName = "Alpha Team";
        var eventId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();

        // Act
        var team = Team.Create(teamName, eventId, creatorId);

        // Assert
        team.Name.Should().Be(teamName);
        team.EventId.Should().Be(eventId);
        team.Score.Value.Should().Be(0);
        team.CurrentStageIndex.Should().Be(0);
    }
}
