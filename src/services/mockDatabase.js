// Initial Seed Data
const SEED_DATA = {
    teams: [
        { id: 't1', name: 'Binary Bandits', score: 0, currentStage: 0, completedQuestions: [], skippedQuestions: [] },
        { id: 't2', name: 'Logic Lords', score: 0, currentStage: 0, completedQuestions: [], skippedQuestions: [] },
        { id: 't3', name: 'Cyber Punks', score: 0, currentStage: 0, completedQuestions: [], skippedQuestions: [] },
    ],
    users: [
        { id: 'u1', username: 'admin', password: 'admin123', role: 'ADMIN' },
        { id: 'u2', username: 'vol1', password: 'vol123', role: 'VOLUNTEER', assignedTeamId: 't1' },
        { id: 'u3', username: 'vol2', password: 'vol123', role: 'VOLUNTEER', assignedTeamId: 't2' },
        { id: 'u4', username: 'part1', password: 'team123', role: 'PARTICIPANT', teamId: 't1' },
        { id: 'u5', username: 'part2', password: 'team123', role: 'PARTICIPANT', teamId: 't2' },
    ],
    questions: [
        {
            id: 'q1',
            title: 'Binary Search Tree',
            description: 'Find the lowest common ancestor of two nodes in a BST.',
            inputs: '[root, p, q]',
            outputs: 'Node',
            points: 100,
            riddleId: 'r1'
        },
        {
            id: 'q2',
            title: 'Two Sum',
            description: 'Find two numbers that add up to a specific target.',
            inputs: '[nums, target]',
            outputs: '[index1, index2]',
            points: 50,
            riddleId: 'r2'
        },
        {
            id: 'q3',
            title: 'Matrix Traversal',
            description: 'Traverse a matrix in spiral order.',
            inputs: '[[1,2,3],[4,5,6]]',
            outputs: '[1,2,3,6,5,4]',
            points: 150,
            riddleId: 'r3'
        },
    ],
    riddles: [
        { id: 'r1', question: 'I speak without a mouth and hear without ears. I have no body, but I come alive with wind.', answer: 'Echo', location: 'Main Hall' },
        { id: 'r2', question: 'The more of this there is, the less you see.', answer: 'Darkness', location: 'Server Room' },
        { id: 'r3', question: 'I have keys but no locks. I have a space but no room. You can enter, but never leave.', answer: 'Keyboard', location: 'Lab 3' },
    ],
    globalState: {
        timerStart: null, // timestamp
        timerDuration: 3600, // seconds
        isRefreshed: false
    }
};

class MockDatabase {
    constructor() {
        this.init();
    }

    init() {
        if (!localStorage.getItem('tt_teams')) {
            console.log('Initializing Seed Data...');
            this.save('tt_teams', SEED_DATA.teams);
            this.save('tt_users', SEED_DATA.users);
            this.save('tt_questions', SEED_DATA.questions);
            this.save('tt_riddles', SEED_DATA.riddles);
            this.save('tt_global', SEED_DATA.globalState);
        }
    }

    get(key) {
        return JSON.parse(localStorage.getItem(key) || '[]');
    }

    save(key, data) {
        localStorage.setItem(key, JSON.stringify(data));
    }

    // Helper to simulate network delay
    async delay(ms = 500) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
}

export const db = new MockDatabase();
