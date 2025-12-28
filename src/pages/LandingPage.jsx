import React, { useEffect, useState } from 'react';
import { motion, useScroll, useTransform } from 'framer-motion';
import { Link } from 'react-router-dom';
import { ArrowRight, Terminal, Code, Cpu, Trophy } from 'lucide-react';
import NeonButton from '../components/ui/NeonButton';
import GlassCard from '../components/ui/GlassCard';
import AnimatedPage from '../components/ui/AnimatedPage';

const LandingPage = () => {
    const { scrollY } = useScroll();
    const opacity = useTransform(scrollY, [0, 300], [1, 0]);
    const scale = useTransform(scrollY, [0, 300], [1, 0.9]);

    const [textIndex, setTextIndex] = useState(0);
    const words = ["SOLVE", "EXPLORE", "CONQUER"];

    useEffect(() => {
        const interval = setInterval(() => {
            setTextIndex((prev) => (prev + 1) % words.length);
        }, 2000);
        return () => clearInterval(interval);
    }, []);

    return (
        <AnimatedPage className="min-h-screen relative overflow-hidden">
            {/* Background Elements */}
            <div className="fixed inset-0 pointer-events-none">
                <div className="absolute top-0 -left-20 w-96 h-96 bg-primary/10 rounded-full blur-[100px]" />
                <div className="absolute bottom-0 -right-20 w-96 h-96 bg-secondary/10 rounded-full blur-[100px]" />
                <div className="absolute inset-0 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] opacity-20" />
            </div>

            {/* Hero Section */}
            <section className="relative h-screen flex flex-col items-center justify-center text-center px-4">
                <motion.div style={{ opacity, scale }} className="z-10 max-w-5xl mx-auto space-y-8">
                    <motion.div
                        initial={{ scale: 0.8, opacity: 0 }}
                        animate={{ scale: 1, opacity: 1 }}
                        transition={{ duration: 0.8, ease: "easeOut" }}
                        className="flex items-center justify-center gap-2 mb-4"
                    >
                        <Terminal className="text-primary h-6 w-6" />
                        <span className="font-mono text-primary tracking-widest text-sm">SYSTEM_READY</span>
                    </motion.div>

                    <h1 className="text-6xl md:text-8xl font-bold tracking-tighter mix-blend-overlay text-white">
                        TECH <span className="text-transparent bg-clip-text bg-gradient-to-r from-primary to-secondary">TREK</span>
                    </h1>

                    <div className="h-12 overflow-hidden">
                        <motion.p
                            key={textIndex}
                            initial={{ y: 40, opacity: 0 }}
                            animate={{ y: 0, opacity: 1 }}
                            exit={{ y: -40, opacity: 0 }}
                            className="text-2xl md:text-3xl font-mono text-text-muted"
                        >
                            {words[textIndex]}
                        </motion.p>
                    </div>

                    <div className="flex flex-col sm:flex-row gap-6 justify-center pt-8">
                        <Link to="/auth/register">
                            <NeonButton size="lg" variant="primary">
                                Initialize Sequence <ArrowRight className="ml-2 h-5 w-5" />
                            </NeonButton>
                        </Link>
                        <Link to="/auth/login">
                            <NeonButton size="lg" variant="ghost">
                                System Login
                            </NeonButton>
                        </Link>
                    </div>
                </motion.div>

                {/* Scroll Indicator */}
                <motion.div
                    animate={{ y: [0, 10, 0] }}
                    transition={{ duration: 2, repeat: Infinity }}
                    className="absolute bottom-10 left-1/2 -translate-x-1/2 text-text-muted/50"
                >
                    <div className="w-6 h-10 border-2 border-current rounded-full flex justify-center pt-2">
                        <div className="w-1 h-3 bg-current rounded-full" />
                    </div>
                </motion.div>
            </section>

            {/* Stats / Features Grid */}
            <section className="container mx-auto px-4 py-32 relative z-10">
                <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                    <GlassCard hoverEffect className="p-8 text-center space-y-4">
                        <div className="w-16 h-16 mx-auto bg-primary/10 rounded-2xl flex items-center justify-center">
                            <Code className="h-8 w-8 text-primary" />
                        </div>
                        <h3 className="text-2xl font-bold">10+ Challenges</h3>
                        <p className="text-text-muted">Algorithmic puzzles designed to test your processing power.</p>
                    </GlassCard>

                    <GlassCard hoverEffect className="p-8 text-center space-y-4">
                        <div className="w-16 h-16 mx-auto bg-secondary/10 rounded-2xl flex items-center justify-center">
                            <Trophy className="h-8 w-8 text-secondary" />
                        </div>
                        <h3 className="text-2xl font-bold">Global Rank</h3>
                        <p className="text-text-muted">Compete against elite developers in real-time.</p>
                    </GlassCard>

                    <GlassCard hoverEffect className="p-8 text-center space-y-4">
                        <div className="w-16 h-16 mx-auto bg-success/10 rounded-2xl flex items-center justify-center">
                            <Cpu className="h-8 w-8 text-success" />
                        </div>
                        <h3 className="text-2xl font-bold">Hardware Prizes</h3>
                        <p className="text-text-muted">Top performers unlock exclusive tech gear.</p>
                    </GlassCard>
                </div>
            </section>

            {/* Rules Marquee (Mock) */}
            <div className="py-12 bg-black/50 overflow-hidden whitespace-nowrap border-y border-white/5">
                <motion.div
                    animate={{ x: [0, -1000] }}
                    transition={{ duration: 20, repeat: Infinity, ease: "linear" }}
                    className="inline-block font-mono text-text-muted/50 text-xl tracking-widest"
                >
                    NO CHEATING // FOLLOW PROTOCOLS // OPTIMIZE CODE // TIME IS TICKING // NO CHEATING // FOLLOW PROTOCOLS // OPTIMIZE CODE // TIME IS TICKING
                </motion.div>
            </div>

        </AnimatedPage>
    );
};

export default LandingPage;
