import React, { useState, useEffect } from 'react';
import { useGame } from '../../context/GameContext';
import { useNavigate } from 'react-router-dom';
import { Users, Clock, Trophy, AlertTriangle, RefreshCw } from 'lucide-react';
import AnimatedPage from '../../components/ui/AnimatedPage';
import GlassCard from '../../components/ui/GlassCard';
import NeonButton from '../../components/ui/NeonButton';
import api from '../../services/api';

const AdminDashboard = () => {
    const { user } = useGame();
    const navigate = useNavigate();
    const [teams, setTeams] = useState([]);
    const [globalState, setGlobalState] = useState(null);

    useEffect(() => {
        if (!user || user.role !== 'ADMIN') navigate('/auth/login');
        loadData();
        const interval = setInterval(loadData, 5000);
        return () => clearInterval(interval);
    }, [user]);

    const loadData = async () => {
        try {
            // Note: Update to actual admin endpoints if they differ
            const response = await api.get('/Admin/teams');
            if (response.data.success) {
                const t = response.data.data || [];
                setTeams(t.sort((a, b) => b.score - a.score));
            }
        } catch (error) {
            console.error("Failed to load admin data", error);
        }
    };

    const handleResetGame = async () => {
        if (confirm("DANGER: This will wipe all progress. Are you sure?")) {
            try {
                await api.post('/Admin/reset');
                window.location.reload();
            } catch (error) {
                console.error("Failed to reset", error);
            }
        }
    }

    return (
        <AnimatedPage className="min-h-screen p-8 bg-gray-900">
            <header className="mb-8 flex justify-between items-center">
                <h1 className="text-3xl font-bold text-white tracking-widest uppercase">
                    <span className="text-primary">System</span> Oversight
                </h1>
                <NeonButton variant="danger" size="sm" onClick={handleResetGame}>
                    <AlertTriangle className="h-4 w-4 mr-2" /> FACTORY RESET SYSTEM
                </NeonButton>
            </header>

            <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
                <GlassCard className="p-6 flex items-center gap-4">
                    <div className="bg-primary/20 p-3 rounded-lg"><Clock className="h-6 w-6 text-primary" /></div>
                    <div>
                        <p className="text-xs text-text-muted uppercase">Global Timer</p>
                        <p className="text-2xl font-mono font-bold text-white">00:44:12</p>
                    </div>
                </GlassCard>
                <GlassCard className="p-6 flex items-center gap-4">
                    <div className="bg-secondary/20 p-3 rounded-lg"><Users className="h-6 w-6 text-secondary" /></div>
                    <div>
                        <p className="text-xs text-text-muted uppercase">Active Units</p>
                        <p className="text-2xl font-mono font-bold text-white">{teams.length}</p>
                    </div>
                </GlassCard>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Leaderboard */}
                <div className="lg:col-span-2">
                    <GlassCard className="p-0 overflow-hidden">
                        <div className="p-6 border-b border-white/5 bg-white/5 flex justify-between items-center">
                            <h2 className="font-bold flex items-center gap-2"><Trophy className="text-yellow-500 h-5 w-5" /> Live Rankings</h2>
                            <span className="text-xs text-green-400 animate-pulse flex items-center gap-1"><span className="w-2 h-2 rounded-full bg-green-500" /> REAL-TIME</span>
                        </div>
                        <table className="w-full text-left bg-black/20">
                            <thead className="text-xs text-text-muted uppercase border-b border-white/5">
                                <tr>
                                    <th className="px-6 py-4">Rank</th>
                                    <th className="px-6 py-4">Unit Name</th>
                                    <th className="px-6 py-4">Stage</th>
                                    <th className="px-6 py-4 text-right">Score</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-white/5">
                                {teams.map((team, idx) => (
                                    <tr key={team.id} className="hover:bg-white/5 transition-colors">
                                        <td className="px-6 py-4 font-mono">
                                            {idx === 0 ? <span className="text-yellow-500 font-bold">#1</span> :
                                                idx === 1 ? <span className="text-gray-300 font-bold">#2</span> :
                                                    idx === 2 ? <span className="text-amber-700 font-bold">#3</span> :
                                                        `#${idx + 1}`}
                                        </td>
                                        <td className="px-6 py-4 font-bold text-white">{team.name}</td>
                                        <td className="px-6 py-4 text-text-muted text-sm">{team.currentStage} / 3</td>
                                        <td className="px-6 py-4 text-right font-mono font-bold text-primary">{team.score}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </GlassCard>
                </div>

                {/* Feed / Logistics */}
                <div className="space-y-6">
                    <GlassCard className="p-6">
                        <h3 className="font-bold mb-4 text-sm uppercase text-text-muted">System Logs</h3>
                        <div className="space-y-3">
                            <div className="text-xs font-mono text-text-muted border-l-2 border-primary pl-3 py-1">
                                <span className="text-white block">Binary Bandits</span>
                                Solved 'Binary Search Tree' (+100pts)
                            </div>
                            <div className="text-xs font-mono text-text-muted border-l-2 border-secondary pl-3 py-1">
                                <span className="text-white block">Logic Lords</span>
                                Checked in at 'Server Room'
                            </div>
                            <div className="text-xs font-mono text-text-muted border-l-2 border-green-500 pl-3 py-1">
                                <span className="text-white block">System</span>
                                Event Initialized
                            </div>
                        </div>
                    </GlassCard>
                </div>
            </div>
        </AnimatedPage>
    );
};

export default AdminDashboard;
