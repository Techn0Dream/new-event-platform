import React, { useState, useEffect } from 'react';
import { CheckCircle, AlertTriangle, SkipForward, MapPin } from 'lucide-react';
import AnimatedPage from '../../components/ui/AnimatedPage';
import GlassCard from '../../components/ui/GlassCard';
import NeonButton from '../../components/ui/NeonButton';
import { useGame } from '../../context/GameContext';
import { GameEngine } from '../../services/gameEngine';
import { useNavigate } from 'react-router-dom';

const VolunteerDashboard = () => {
    const { gameState, team, refreshGameState, user } = useGame();
    const navigate = useNavigate();
    const [isLoading, setIsLoading] = useState(false);

    useEffect(() => {
        if (!user || user.role !== 'VOLUNTEER') navigate('/auth/login');
        const interval = setInterval(() => {
            if (team) refreshGameState(team.id);
        }, 5000);
        return () => clearInterval(interval);
    }, [user, team]);

    if (!gameState) return <div className="p-20 text-center">Loading Field Data...</div>;

    const { status, riddle, stageIndex } = gameState;

    const handleVerify = async () => {
        if (confirm("Confirm: Participant has verbally answered correctly?")) {
            setIsLoading(true);
            await GameEngine.verifyRiddle(team.id, stageIndex);
            await refreshGameState(team.id);
            setIsLoading(false);
        }
    };

    const handleSkip = async () => {
        if (confirm("WARNING: Skipping question penalty will apply. Continue?")) {
            setIsLoading(true);
            await GameEngine.skipQuestion(team.id);
            await refreshGameState(team.id);
            setIsLoading(false);
        }
    }

    return (
        <AnimatedPage className="min-h-screen p-4 md:p-8 bg-gray-900">
            <header className="mb-8 flex justify-between items-center p-4 bg-secondary/10 border border-secondary/20 rounded-xl">
                <div>
                    <h1 className="text-xl font-bold text-secondary">FIELD AGENT: {user.username}</h1>
                    <p className="text-sm text-text-muted">ASSIGNED UNIT: <span className="text-white font-bold">{team.name}</span></p>
                </div>
                <div className="text-right">
                    <span className={`px-3 py-1 rounded text-xs font-bold ${status === 'LOCKED' ? 'bg-orange-500/20 text-orange-400' : 'bg-green-500/20 text-green-400'}`}>
                        STATUS: {status}
                    </span>
                </div>
            </header>

            <div className="max-w-2xl mx-auto space-y-6">

                {/* Riddle Verification Card */}
                {status === 'LOCKED' && riddle && (
                    <GlassCard className="border-orange-500/30 bg-orange-500/5">
                        <div className="p-6 border-b border-white/5 flex justify-between items-center">
                            <h2 className="text-xl font-bold text-orange-400 flex items-center gap-2">
                                <MapPin /> Location Verification
                            </h2>
                            <span className="text-xs font-mono text-text-muted">CHECKPOINT #{stageIndex + 1}</span>
                        </div>

                        <div className="p-6 space-y-6">
                            <div>
                                <p className="text-xs text-text-muted uppercase tracking-wider mb-1">Riddle (Visible to Team)</p>
                                <p className="text-lg italic text-white bg-black/30 p-4 rounded border border-white/5">
                                    "{riddle.question}"
                                </p>
                            </div>

                            <div>
                                <p className="text-xs text-text-muted uppercase tracking-wider mb-1">Target Location</p>
                                <p className="text-white font-bold">{riddle.location}</p>
                            </div>

                            <div className="bg-green-900/20 p-4 rounded border border-green-500/30">
                                <p className="text-xs text-green-400 uppercase tracking-wider mb-2">Required Answer Key</p>
                                <p className="text-2xl font-bold text-white tracking-widest">{riddle.answer.toUpperCase()}</p>
                            </div>

                            <NeonButton onClick={handleVerify} isLoading={isLoading} className="w-full bg-green-600 hover:bg-green-500 text-white border-none">
                                <CheckCircle className="mr-2" /> VERIFY & UNLOCK QUESTION
                            </NeonButton>
                        </div>
                    </GlassCard>
                )}

                {/* Status Monitor */}
                {status === 'ACTIVE' && (
                    <GlassCard className="border-blue-500/30 bg-blue-500/5 p-8 text-center">
                        <div className="mb-6">
                            <div className="w-16 h-16 bg-blue-500/20 rounded-full flex items-center justify-center mx-auto animate-pulse">
                                <Terminal className="h-8 w-8 text-blue-400" />
                            </div>
                        </div>
                        <h2 className="text-2xl font-bold text-white">Team is Solving...</h2>
                        <p className="text-text-muted mb-8">Do not interfere with the logic process.</p>

                        <div className="pt-8 border-t border-white/5">
                            <p className="text-xs text-red-400 mb-2">EMERGENCY OVERRIDE</p>
                            <NeonButton onClick={handleSkip} variant="danger" size="sm" isLoading={isLoading} className="w-full">
                                <SkipForward className="mr-2 h-4 w-4" /> SKIP THIS QUESTION
                            </NeonButton>
                        </div>
                    </GlassCard>
                )}

                {status === 'COMPLETED' && (
                    <div className="text-center p-12">
                        <h2 className="text-3xl font-bold text-success">Unit Finished</h2>
                        <p className="text-text-muted">Return to base.</p>
                    </div>
                )}

            </div>
        </AnimatedPage>
    );
};

export default VolunteerDashboard;
