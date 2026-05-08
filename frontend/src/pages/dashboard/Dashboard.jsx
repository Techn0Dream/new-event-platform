import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Terminal, MapPin, Clock, Award, ChevronRight } from 'lucide-react';
import { motion } from 'framer-motion';
import AnimatedPage from '../../components/ui/AnimatedPage';
import GlassCard from '../../components/ui/GlassCard';
import NeonButton from '../../components/ui/NeonButton';
import ProgressRing from '../../components/ui/ProgressRing';

const Dashboard = () => {
    // Mock user state
    const user = { name: "Neo", score: 1250, rank: 4 };
    const currentChallenge = {
        title: "The Binary Siege",
        difficulty: "Hard",
        timeLimit: "45:00",
        type: "Algorithm"
    };

    return (
        <AnimatedPage className="min-h-screen p-6 md:p-12 space-y-8 pb-24">
            <header className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight text-white mb-1">
                        Welcome back, <span className="text-primary">{user.name}</span>
                    </h1>
                    <div className="flex items-center gap-2 text-text-muted font-mono text-sm">
                        <span className="w-2 h-2 rounded-full bg-green-500 animate-pulse" />
                        Node Online
                    </div>
                </div>

                <div className="flex gap-4">
                    <GlassCard className="px-6 py-3 flex items-center gap-3">
                        <div className="text-right">
                            <p className="text-xs text-text-muted uppercase tracking-wider">Current Rank</p>
                            <p className="text-xl font-bold text-secondary font-mono">#{user.rank}</p>
                        </div>
                        <Award className="h-8 w-8 text-secondary/50" />
                    </GlassCard>
                </div>
            </header>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Main Challenge Card */}
                <div className="lg:col-span-2">
                    <GlassCard className="h-full p-8 relative overflow-hidden group">
                        <div className="absolute top-0 right-0 p-4 opacity-10 group-hover:opacity-20 transition-opacity">
                            <Terminal className="h-64 w-64 rotate-12" />
                        </div>

                        <div className="relative z-10 space-y-6">
                            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-primary/10 border border-primary/20 text-primary text-xs font-mono">
                                <span className="animate-pulse w-2 h-2 rounded-full bg-primary"></span>
                                ACTIVE MISSION
                            </div>

                            <div>
                                <h2 className="text-4xl font-bold text-white mb-2">{currentChallenge.title}</h2>
                                <p className="text-text-muted max-w-xl">
                                    Intercept the incoming data stream and decrypt the binary sequence before the firewall resets. Optimization is key.
                                </p>
                            </div>

                            <div className="flex flex-wrap gap-6 py-4">
                                <div className="flex items-center gap-3 text-sm font-mono text-text-main">
                                    <div className="p-2 bg-white/5 rounded-lg">
                                        <Clock className="w-5 h-5 text-secondary" />
                                    </div>
                                    <div>
                                        <p className="text-text-muted text-xs">Time Limit</p>
                                        <p>{currentChallenge.timeLimit}</p>
                                    </div>
                                </div>
                                <div className="flex items-center gap-3 text-sm font-mono text-text-main">
                                    <div className="p-2 bg-white/5 rounded-lg">
                                        <MapPin className="w-5 h-5 text-success" />
                                    </div>
                                    <div>
                                        <p className="text-text-muted text-xs">Difficulty</p>
                                        <p className="text-success">{currentChallenge.difficulty}</p>
                                    </div>
                                </div>
                            </div>

                            <Link to="/challenge/1">
                                <NeonButton size="lg" className="w-full sm:w-auto shine-effect">
                                    Initialize Challenge <ChevronRight className="ml-2 h-5 w-5" />
                                </NeonButton>
                            </Link>
                        </div>
                    </GlassCard>
                </div>

                {/* Progress & Sidebar */}
                <div className="space-y-8">
                    <GlassCard className="p-8 flex flex-col items-center justify-center space-y-4">
                        <h3 className="font-bold text-lg mb-2">System Clearance</h3>
                        <ProgressRing progress={65} />
                        <p className="text-center text-sm text-text-muted">3/5 Modules Complete</p>
                    </GlassCard>

                    <GlassCard className="p-0 overflow-hidden">
                        <div className="p-4 border-b border-white/5 bg-white/5">
                            <h3 className="font-bold text-sm uppercase tracking-wider">Live Feed</h3>
                        </div>
                        <div className="p-4 space-y-4">
                            {[1, 2, 3].map((i) => (
                                <div key={i} className="flex gap-3 items-start text-sm">
                                    <div className="w-8 h-8 rounded-full bg-white/10 flex items-center justify-center flex-shrink-0 font-mono text-xs">
                                        {i === 1 ? 'A1' : i === 2 ? 'B2' : 'C3'}
                                    </div>
                                    <div>
                                        <p className="text-text-main"><span className="text-primary font-bold">User_{400 + i}</span> solved "The Matrix"</p>
                                        <p className="text-text-muted text-xs flex items-center gap-1"><Clock className="w-3 h-3" /> {i * 2}m ago</p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </GlassCard>
                </div>
            </div>
        </AnimatedPage>
    );
};

export default Dashboard;
