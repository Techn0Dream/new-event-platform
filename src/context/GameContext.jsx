import React, { createContext, useContext, useState, useEffect } from 'react';
import { AuthService } from '../services/authService';
import { GameEngine } from '../services/gameEngine';

const GameContext = createContext();

export const GameProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [team, setTeam] = useState(null); // The team the user belongs to OR is assigned to
    const [gameState, setGameState] = useState(null); // { status, riddle, question, team }
    const [isLoading, setIsLoading] = useState(true);

    // Load session on mount
    useEffect(() => {
        const session = AuthService.getSession();
        if (session) {
            setUser(session.user);
            setTeam(session.team);
            if (session.user.role === 'PARTICIPANT' || session.user.role === 'VOLUNTEER') {
                refreshGameState(session.team?.id);
            }
        }
        setIsLoading(false);
    }, []);

    const login = async (username, password) => {
        const session = await AuthService.login(username, password);
        setUser(session.user);
        setTeam(session.team);
        if (session.team) {
            await refreshGameState(session.team.id);
        }
        return session;
    };

    const logout = () => {
        AuthService.logout();
        setUser(null);
        setTeam(null);
        setGameState(null);
    };

    const refreshGameState = async (teamId) => {
        if (!teamId) return;
        try {
            const state = await GameEngine.getTeamState(teamId);
            setGameState(state);
            // Also update local team object if changed
            setTeam(prev => ({ ...prev, ...state.team }));
        } catch (e) {
            console.error("Failed to refresh game state", e);
        }
    };

    return (
        <GameContext.Provider value={{
            user,
            team,
            gameState,
            isLoading,
            login,
            logout,
            refreshGameState
        }}>
            {children}
        </GameContext.Provider>
    );
};

export const useGame = () => useContext(GameContext);
