import React, { createContext, useContext, useState, useEffect, useRef } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { AuthService } from '../services/authService';
import { GameEngine } from '../services/gameEngine';

const GameContext = createContext();

export const GameProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [team, setTeam] = useState(null); // The team the user belongs to OR is assigned to
    const [gameState, setGameState] = useState(null); // { status, riddle, question, team }
    const [isLoading, setIsLoading] = useState(true);
    const connectionRef = useRef(null);

    // Load session on mount
    useEffect(() => {
        const init = async () => {
            const session = await AuthService.restoreSession();
            if (session) {
                setUser(session.user);
                setTeam(session.team);
                if (session.user.role === 'PARTICIPANT' || session.user.role === 'VOLUNTEER') {
                    await refreshGameState(session.team?.id);
                }
            }
            setIsLoading(false);
        };
        init();
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

    const logout = async () => {
        if (connectionRef.current) {
            await connectionRef.current.stop();
            connectionRef.current = null;
        }
        await AuthService.logout();
        setUser(null);
        setTeam(null);
        setGameState(null);
    };

    // Initialize SignalR Connection
    useEffect(() => {
        if (!team?.id || !user) return;

        const connectSignalR = async () => {
            const connection = new HubConnectionBuilder()
                .withUrl(`http://localhost:5283/hubs/game`, {
                    // Note: SignalR requires the token or credentials for authorization
                    withCredentials: true 
                })
                .configureLogging(LogLevel.Information)
                .withAutomaticReconnect()
                .build();

            connection.on("GameStateUpdated", () => {
                console.log("Real-time update received! Refreshing state...");
                refreshGameState(team.id);
            });

            try {
                await connection.start();
                console.log("SignalR Connected to GameHub");
                // The backend automatically adds the user to their team group based on their JWT claims
                connectionRef.current = connection;
            } catch (err) {
                console.error("SignalR Connection Error: ", err);
            }
        };

        connectSignalR();

        return () => {
            if (connectionRef.current) {
                connectionRef.current.stop();
                connectionRef.current = null;
            }
        };
    }, [team?.id, user]);

    const refreshGameState = async (teamId) => {
        if (!teamId) return;
        try {
            const state = await GameEngine.getTeamState(teamId);
            setGameState(state);
            if (state && state.team) {
                setTeam(prev => ({ ...prev, ...state.team }));
            }
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
