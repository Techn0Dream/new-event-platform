import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { User, Mail, Lock, Flag, ArrowRight, ArrowLeft } from 'lucide-react';
import AnimatedPage from '../../components/ui/AnimatedPage';
import GlassCard from '../../components/ui/GlassCard';
import AnimatedInput from '../../components/ui/AnimatedInput';
import NeonButton from '../../components/ui/NeonButton';
import { motion } from 'framer-motion';

const Register = () => {
    const navigate = useNavigate();
    const [step, setStep] = useState(1);
    const [isLoading, setIsLoading] = useState(false);
    const [isTeam, setIsTeam] = useState(false);

    const handleNext = () => setStep(step + 1);
    const handleBack = () => setStep(step - 1);

    const handleSubmit = (e) => {
        e.preventDefault();
        setIsLoading(true);
        setTimeout(() => {
            navigate('/auth/login');
        }, 1500);
    };

    return (
        <AnimatedPage className="min-h-screen flex items-center justify-center p-4 relative overflow-hidden">
            <div className="absolute inset-0 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] opacity-10" />

            <GlassCard className="w-full max-w-lg p-0 overflow-hidden relative z-10">
                {/* Progress Bar */}
                <div className="h-1 bg-white/5 w-full">
                    <motion.div
                        className="h-full bg-gradient-to-r from-primary to-secondary"
                        initial={{ width: '0%' }}
                        animate={{ width: step === 1 ? '50%' : '100%' }}
                        transition={{ duration: 0.5 }}
                    />
                </div>

                <div className="p-8">
                    <div className="text-center mb-8">
                        <h2 className="text-3xl font-bold tracking-tight">Identity Creation</h2>
                        <p className="text-text-muted mt-2 font-mono text-sm">Step {step} of 2: {step === 1 ? 'Basic Info' : 'Team Configuration'}</p>
                    </div>

                    <form onSubmit={handleSubmit} className="space-y-6">
                        {step === 1 ? (
                            <motion.div
                                initial={{ opacity: 0, x: -20 }}
                                animate={{ opacity: 1, x: 0 }}
                                exit={{ opacity: 0, x: 20 }}
                                className="space-y-4"
                            >
                                <AnimatedInput label="Codename" placeholder="Neo" icon={User} />
                                <AnimatedInput label="Email Frequency" type="email" placeholder="neo@matrix.io" icon={Mail} />
                                <AnimatedInput label="Passcode" type="password" placeholder="••••••••" icon={Lock} />

                                <div className="flex justify-end pt-4">
                                    <NeonButton type="button" onClick={handleNext} variant="secondary">
                                        Continue Protocol <ArrowRight className="ml-2 h-4 w-4" />
                                    </NeonButton>
                                </div>
                            </motion.div>
                        ) : (
                            <motion.div
                                initial={{ opacity: 0, x: 20 }}
                                animate={{ opacity: 1, x: 0 }}
                                exit={{ opacity: 0, x: -20 }}
                                className="space-y-6"
                            >
                                {/* Toggle */}
                                <div className="flex p-1 bg-white/5 rounded-lg border border-white/10">
                                    <button
                                        type="button"
                                        onClick={() => setIsTeam(false)}
                                        className={`flex-1 py-2 text-sm font-medium rounded-md transition-all ${!isTeam ? 'bg-primary/20 text-primary shadow-lg' : 'text-text-muted hover:text-white'}`}
                                    >
                                        Solo Runner
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setIsTeam(true)}
                                        className={`flex-1 py-2 text-sm font-medium rounded-md transition-all ${isTeam ? 'bg-secondary/20 text-secondary shadow-lg' : 'text-text-muted hover:text-white'}`}
                                    >
                                        Squad Mode
                                    </button>
                                </div>

                                {isTeam && (
                                    <div className="space-y-4 animate-in fade-in slide-in-from-top-2">
                                        <AnimatedInput label="Squad Name" placeholder="Recursion Rebels" icon={Flag} />
                                        <AnimatedInput label="Teammate Email (Optional)" placeholder="trinity@matrix.io" icon={Mail} />
                                    </div>
                                )}

                                {!isTeam && (
                                    <div className="p-4 rounded-lg bg-primary/5 border border-primary/10 text-center">
                                        <p className="text-sm text-primary">Running solo maximizes glory but increases difficulty.</p>
                                    </div>
                                )}

                                <div className="flex gap-4 pt-4">
                                    <NeonButton type="button" onClick={handleBack} variant="ghost">
                                        <ArrowLeft className="mr-2 h-4 w-4" /> Back
                                    </NeonButton>
                                    <NeonButton type="submit" variant="primary" className="flex-1" isLoading={isLoading}>
                                        Initialize
                                    </NeonButton>
                                </div>
                            </motion.div>
                        )}
                    </form>
                </div>
            </GlassCard>
        </AnimatedPage>
    );
};

export default Register;
