import React, { useState, useEffect } from 'react';
import { Lock, MapPin, Terminal, Clock, RefreshCw, Trophy } from 'lucide-react';
import AnimatedPage from '../../components/ui/AnimatedPage';
import GlassCard from '../../components/ui/GlassCard';
import NeonButton from '../../components/ui/NeonButton';
import ProgressRing from '../../components/ui/ProgressRing';
import { useGame } from '../../context/GameContext';
import { GameEngine } from '../../services/gameEngine';
import { useNavigate } from 'react-router-dom';

const ParticipantDashboard = () => {
    const { gameState, team, refreshGameState, user } = useGame();
    const navigate = useNavigate();
    const [code, setCode] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        if (!user) navigate('/auth/login');
        const interval = setInterval(() => {
            if (team) refreshGameState(team.id);
        }, 5000); // Auto-refresh state every 5s
        return () => clearInterval(interval);
    }, [user, team]);

    if (!gameState) return <div className="p-20 text-center">Loading Mission Data...</div>;

    const { status, riddle, question, stageIndex } = gameState;

    const handleAnswerSubmit = async () => {
        setIsSubmitting(true);
        try {
            await GameEngine.submitDistance(team.id, code);
            refreshGameState(team.id);
            setCode('');
        } catch (e) {
            alert('Submission Failed: ' + e.message);
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <AnimatedPage className="min-h-screen p-4 md:p-8 pb-24">
            {/* Header */}
            <div className="flex justify-between items-center mb-8">
                <div>
                    <h1 className="text-2xl font-bold font-mono text-primary">MISSION CONTROL</h1>
                    <p className="text-text-muted text-sm">TEAM: <span className="text-white">{team.name}</span></p>
                </div>
                <div className="text-right font-mono">
                    <p className="text-xs text-text-muted">CURRENT SCORE</p>
                    <p className="text-2xl font-bold text-success">{team.score}</p>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Main Action Area */}
                <div className="lg:col-span-2 space-y-6">
                    {/* Progress Bar */}
                    <div className="w-full bg-white/5 rounded-full h-2 overflow-hidden mb-4">
                        <div
                            className="h-full bg-primary transition-all duration-500"
                            style={{ width: `${(stageIndex / 3) * 100}%` }} // Mock max 3 stages
                        />
                    </div>

                    {status === 'LOCKED' && riddle && (
                        <GlassCard className="p-8 border-primary/20 bg-primary/5 text-center relative overflow-hidden">
                            <div className="absolute top-0 left-0 w-full h-1 bg-gradient-to-r from-transparent via-primary to-transparent animate-pulse" />
                            <Lock className="h-16 w-16 mx-auto mb-6 text-primary animate-bounce-slow" />
                            <h2 className="text-2xl font-bold text-white mb-2">ACCESS RESTRICTED</h2>
                            <p className="text-text-muted mb-8 max-w-md mx-auto leading-relaxed">
                                Good work reaching this checkpoint, Agent. To unlock the next mission protocol, you must locate the physical scanner and provide the verbal key to the Field Agent nearby.
                            </p>

                            <div className="bg-black/40 p-6 rounded-xl border border-white/10 max-w-lg mx-auto transform hover:scale-105 transition-transform duration-300">
                                <p className="text-xs font-mono text-primary mb-2 uppercase tracking-widest">Target Location Riddle</p>
                                <p className="text-xl font-serif italic text-white">"{riddle.question}"</p>
                            </div>

                            <div className="mt-8 flex justify-center gap-2 items-center text-xs text-text-muted animate-pulse">
                                <MapPin className="h-4 w-4" /> Waiting for Volunteer Verification...
                            </div>
                        </GlassCard>
                    )}

                    {status === 'ACTIVE' && question && (
                        <GlassCard className="p-0 overflow-hidden border-success/20">
                            <div className="p-6 border-b border-white/5 bg-success/5 flex justify-between items-center">
                                <div>
                                    <span className="text-xs font-bold bg-success/20 text-success px-2 py-1 rounded">UNLOCKED</span>
                                    <h2 className="text-xl font-bold mt-2">{question.title}</h2>
                                </div>
                                <div className="text-right">
                                    <p className="text-xs text-text-muted">POINTS</p>
                                    <p className="font-mono font-bold text-lg">{question.points}</p>
                                </div>
                            </div>

                            <div className="p-6 space-y-6">
                                <p className="text-text-muted">{question.description}</p>

                                <div className="grid grid-cols-2 gap-4">
                                    <div className="p-3 bg-black/40 rounded border border-white/5">
                                        <p className="text-xs text-secondary mb-1">INPUT</p>
                                        <code className="text-sm font-mono text-text-main">{question.inputs}</code>
                                    </div>
                                    <div className="p-3 bg-black/40 rounded border border-white/5">
                                        <p className="text-xs text-primary mb-1">OUTPUT</p>
                                        <code className="text-sm font-mono text-text-main">{question.outputs}</code>
                                    </div>
                                </div>

                                <div className="pt-4 border-t border-white/5">
                                    <textarea
                                        value={code}
                                        onChange={(e) => setCode(e.target.value)}
                                        placeholder="// Paste your solution code here..."
                                        className="w-full h-40 bg-black/50 rounded-lg p-4 font-mono text-sm border border-white/10 focus:border-primary/50 outline-none transition-colors"
                                    />
                                    <div className="flex justify-end mt-4">
                                        <NeonButton onClick={handleAnswerSubmit} isLoading={isSubmitting} variant="primary">
                                            <Terminal className="mr-2 h-4 w-4" /> Execute & Submit
                                        </NeonButton>
                                    </div>
                                </div>
                            </div>
                        </GlassCard>
                    )}

                    {status === 'COMPLETED' && (
                        <GlassCard className="p-12 text-center bg-gradient-to-b from-success/5 to-success/10 border-success/30">
                            <div className="mb-6 inline-block p-4 rounded-full bg-success/20 animate-bounce-slow">
                                <Trophy className="h-12 w-12 text-success" />
                            </div>
                            <h2 className="text-4xl font-bold text-white mb-4">MISSION ACCOMPLISHED</h2>
                            <p className="text-xl text-text-muted max-w-2xl mx-auto">
                                Outstanding performance! All protocols have been successfully executed. Return to base for debriefing and final ranking.
                            </p>
                            <p className="mt-8 text-6xl font-bold text-success font-mono drop-shadow-[0_0_15px_rgba(16,185,129,0.5)]">{team.score} PTS</p>
                        </GlassCard>
                    )}
                </div>

                {/* Sidebar Info */}
                <div className="space-y-6">
                    <GlassCard className="p-6">
                        <h3 className="font-bold mb-4 text-xs uppercase tracking-widest text-text-muted">Event Timer</h3>
                        <div className="text-4xl font-mono font-bold text-white flex items-center gap-2">
                            <Clock className="size-8 text-primary" /> 00:45:00
                        </div>
                    </GlassCard>

                    <GlassCard className="p-6">
                        <h3 className="font-bold mb-4 text-xs uppercase tracking-widest text-text-muted">Stage Check</h3>
                        <div className="flex justify-between items-center mb-2">
                            <span>Stage {stageIndex + 1} / 3</span>
                            <span className="text-primary">{Math.round((stageIndex / 3) * 100)}%</span>
                        </div>
                        <ProgressRing progress={Math.round((stageIndex / 3) * 100)} radius={40} stroke={6} />
                    </GlassCard>

                    <button onClick={() => refreshGameState(team.id)} className="w-full py-3 text-xs font-mono text-text-muted hover:text-white flex items-center justify-center gap-2">
                        <RefreshCw className="size-3" /> Force Synchronize
                    </button>
                </div>
            </div>
        </AnimatedPage>
    );
};

export default ParticipantDashboard;
