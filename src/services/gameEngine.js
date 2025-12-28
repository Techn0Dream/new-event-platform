import { db } from './mockDatabase';

export const GameEngine = {
    // Get full dashboard state for a team
    async getTeamState(teamId) {
        await db.delay(300);
        const teams = db.get('tt_teams');
        const team = teams.find(t => t.id === teamId);
        const questions = db.get('tt_questions');
        const riddles = db.get('tt_riddles');

        if (!team) throw new Error('Team not found');

        // Logic: 
        // currentStage determines which question index they are on.
        // If currentStage >= questions.length, they are finished.

        // We need to return:
        // 1. Current Riddle (if locked) OR Current Question (if unlocked)
        // 2. Score
        // 3. Status (LOCKED | ACTIVE | COMPLETED)

        const isFinished = team.currentStage >= questions.length;

        if (isFinished) {
            return { status: 'COMPLETED', team };
        }

        const currentQ = questions[team.currentStage];
        const currentRiddle = riddles.find(r => r.id === currentQ.riddleId);

        // In a real DB, we'd store "isUnlocked" flag. 
        // For this mock, let's assume if they haven't submitted the question, it's either LOCKED or ACTIVE.
        // We need a persistent "unlocked" state for the current stage.
        // Let's modify the team object to store `unlockedStages: [0, 1]` etc

        const isUnlocked = team.unlockedStages && team.unlockedStages.includes(team.currentStage);

        return {
            status: isUnlocked ? 'ACTIVE' : 'LOCKED',
            stageIndex: team.currentStage,
            riddle: currentRiddle,
            question: isUnlocked ? currentQ : null,
            team
        };
    },

    // Helper to verify riddle (Volunteer Action)
    async verifyRiddle(teamId, stageIndex) {
        await db.delay(500);
        const teams = db.get('tt_teams');
        const teamIndex = teams.findIndex(t => t.id === teamId);
        if (teamIndex === -1) throw new Error('Team not found');

        const team = teams[teamIndex];
        if (!team.unlockedStages) team.unlockedStages = [];

        if (!team.unlockedStages.includes(stageIndex)) {
            team.unlockedStages.push(stageIndex);
            db.save('tt_teams', teams);
        }
        return true;
    },

    // Submit Answer (Participant Action)
    async submitDistance(teamId, code) {
        await db.delay(1500);
        // Mock validation: Always true for now unless empty
        if (!code || code.length < 10) throw new Error('Code too short or invalid');

        const teams = db.get('tt_teams');
        const teamIndex = teams.findIndex(t => t.id === teamId);
        const team = teams[teamIndex];

        const questions = db.get('tt_questions');
        const currentPoints = questions[team.currentStage].points;

        // Update Team
        team.score += currentPoints;
        team.completedQuestions.push({ stage: team.currentStage, points: currentPoints });
        team.currentStage += 1; // Advance

        db.save('tt_teams', teams);
        return { success: true, points: currentPoints };
    },

    // Skip Question (Volunteer Action)
    async skipQuestion(teamId) {
        await db.delay(500);
        const teams = db.get('tt_teams');
        const teamIndex = teams.findIndex(t => t.id === teamId);
        const team = teams[teamIndex];

        team.skippedQuestions.push(team.currentStage);
        team.currentStage += 1;

        db.save('tt_teams', teams);
        return true;
    }
};
