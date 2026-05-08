import api from './api';

export const GameEngine = {
    async getTeamState(teamId) {
        try {
            const response = await api.get(`/Game/state`);
            if (response.data.success) {
                return response.data.data;
            }
            throw new Error(response.data.message || 'Failed to get team state');
        } catch (error) {
            console.error('getTeamState error:', error);
            throw error;
        }
    },

    async verifyRiddle(teamId, stageIndex) {
        try {
            const response = await api.post(`/Game/team/${teamId}/verify-riddle`, { stageIndex });
            return response.data.success;
        } catch (error) {
            console.error('verifyRiddle error:', error);
            throw new Error(error.response?.data?.message || 'Failed to verify riddle');
        }
    },

    async submitDistance(teamId, code) {
        try {
            const response = await api.post(`/Game/team/${teamId}/submit`, {
                code,
                language: 'javascript' // Assuming language, can be passed dynamically
            });
            if (response.data.success) {
                return { success: true, points: response.data.data.pointsAwarded || 0 };
            }
            throw new Error(response.data.message || 'Submission failed');
        } catch (error) {
            console.error('submitDistance error:', error);
            throw new Error(error.response?.data?.message || 'Submission failed');
        }
    },

    async skipQuestion(teamId) {
        try {
            const response = await api.post(`/Game/team/${teamId}/skip`);
            return response.data.success;
        } catch (error) {
            console.error('skipQuestion error:', error);
            throw new Error(error.response?.data?.message || 'Failed to skip question');
        }
    }
};
