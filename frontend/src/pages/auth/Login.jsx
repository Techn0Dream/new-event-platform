import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Lock, Mail, Terminal, ArrowRight, ShieldAlert } from 'lucide-react';
import AnimatedPage from '../../components/ui/AnimatedPage';
import GlassCard from '../../components/ui/GlassCard';
import AnimatedInput from '../../components/ui/AnimatedInput';
import NeonButton from '../../components/ui/NeonButton';
import { useGame } from '../../context/GameContext';

const Login = () => {
    const navigate = useNavigate();
    const { login } = useGame();
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState(null);
    const [isLoading, setIsLoading] = useState(false);
    const [mode, setMode] = useState('PARTICIPANT'); // PARTICIPANT | VOLUNTEER | ADMIN

    const handleLogin = async (e) => {
        e.preventDefault();
        setIsLoading(true);
        setError(null);
        try {
            const session = await login(username, password);
            // Basic Role Check
            if (session.user.role !== mode && mode !== 'ADMIN') { // Admin can log in generally, but for strict UI let's warn
                // This is just a UI mode filter, the service validates creds
            }

            if (session.user.role === 'ADMIN') navigate('/admin');
            else if (session.user.role === 'VOLUNTEER') navigate('/volunteer');
            else navigate('/mission/control');

        } catch (err) {
            setError('Access Denied: Invalid Credentials');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <AnimatedPage className="min-h-screen flex flex-col items-center justify-center p-4 relative overflow-hidden">
            {/* Background */}
            <div className="absolute inset-0 pointer-events-none">
                <div className={`absolute top-1/4 left-1/4 w-96 h-96 rounded-full blur-[100px] transition-colors duration-1000 ${mode === 'PARTICIPANT' ? 'bg-primary/20' : mode === 'VOLUNTEER' ? 'bg-secondary/20' : 'bg-red-500/10'}`} />
            </div>

            <div className="mb-8 flex gap-4 p-1 bg-white/5 rounded-xl border border-white/10 relative z-10 backdrop-blur-md">
                <button onClick={() => setMode('PARTICIPANT')} className={`px-4 py-2 rounded-lg text-sm font-bold transition-all ${mode === 'PARTICIPANT' ? 'bg-primary text-background' : 'text-text-muted hover:text-white'}`}>PARTICIPANT</button>
                <button onClick={() => setMode('VOLUNTEER')} className={`px-4 py-2 rounded-lg text-sm font-bold transition-all ${mode === 'VOLUNTEER' ? 'bg-secondary text-background' : 'text-text-muted hover:text-white'}`}>VOLUNTEER</button>
                <button onClick={() => setMode('ADMIN')} className={`px-4 py-2 rounded-lg text-sm font-bold transition-all ${mode === 'ADMIN' ? 'bg-white text-black' : 'text-text-muted hover:text-white'}`}>ADMIN</button>
            </div>

            <GlassCard className="w-full max-w-md p-8 relative z-10 border-t-2 border-t-current" style={{ color: mode === 'PARTICIPANT' ? 'var(--color-primary)' : mode === 'VOLUNTEER' ? 'var(--color-secondary)' : 'white' }}>
                <div className="text-center mb-8">
                    <div className={`w-12 h-12 rounded-xl flex items-center justify-center mx-auto mb-4 border border-current bg-current/10`}>
                        <Lock className="h-6 w-6" />
                    </div>
                    <h2 className="text-3xl font-bold tracking-tight text-white mb-1">
                        {mode === 'PARTICIPANT' ? 'Mission Access' : mode === 'VOLUNTEER' ? 'Field Control' : 'System Oversight'}
                    </h2>
                    <p className="text-text-muted font-mono text-xs">SECURE TERMINAL // {mode}_NODE</p>
                </div>

                <form onSubmit={handleLogin} className="space-y-6">
                    <div className="space-y-4">
                        <AnimatedInput
                            value={username} onChange={e => setUsername(e.target.value)}
                            label="Username" placeholder="user_id" icon={Terminal}
                        />
                        <AnimatedInput
                            value={password} onChange={e => setPassword(e.target.value)}
                            label="Passkey" type="password" placeholder="••••••••" icon={Lock}
                        />
                    </div>

                    {error && (
                        <div className="p-3 bg-error/10 border border-error/50 rounded-lg flex items-center gap-2 text-error text-sm font-bold">
                            <ShieldAlert className="h-4 w-4" /> {error}
                        </div>
                    )}

                    <NeonButton type="submit" variant={mode === 'PARTICIPANT' ? 'primary' : mode === 'VOLUNTEER' ? 'secondary' : 'ghost'} className="w-full" isLoading={isLoading}>
                        Initialize Session <ArrowRight className="ml-2 h-4 w-4" />
                    </NeonButton>
                </form>

                <div className="mt-6 pt-6 border-t border-white/5 text-center px-4">
                    <p className="text-text-muted text-xs font-mono">
                        {mode === 'PARTICIPANT' ? 'Only Team Leaders may access this terminal.' : 'Unauthorized access will be logged and reported.'}
                    </p>
                </div>
            </GlassCard>
        </AnimatedPage>
    );
};

export default Login;
