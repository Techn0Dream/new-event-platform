import { db } from './mockDatabase';

export const AuthService = {
    async login(username, password) {
        await db.delay(800);
        const users = db.get('tt_users');
        const user = users.find(u => u.username === username && u.password === password);

        if (!user) {
            throw new Error('Invalid credentials');
        }

        // Attach team details if applicable
        let team = null;
        if (user.teamId) {
            const teams = db.get('tt_teams');
            team = teams.find(t => t.id === user.teamId);
        } else if (user.assignedTeamId) {
            const teams = db.get('tt_teams');
            team = teams.find(t => t.id === user.assignedTeamId);
        }

        const session = { user, team, token: 'mock-jwt-' + Date.now() };
        localStorage.setItem('tt_session', JSON.stringify(session));
        return session;
    },

    logout() {
        localStorage.removeItem('tt_session');
    },

    getSession() {
        return JSON.parse(localStorage.getItem('tt_session'));
    }
};
