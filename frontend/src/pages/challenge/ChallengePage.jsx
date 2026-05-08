import React, { useState } from 'react';
import Editor from '@monaco-editor/react';
import { motion } from 'framer-motion';
import { Play, CheckCircle, RefreshCcw, AlertCircle } from 'lucide-react';
import AnimatedPage from '../../components/ui/AnimatedPage';
import NeonButton from '../../components/ui/NeonButton';
import GlassCard from '../../components/ui/GlassCard';

const ChallengePage = () => {
    const [code, setCode] = useState("// Write your solution algorithm here\nfunction solve(input) {\n  return input;\n}");
    const [activeTab, setActiveTab] = useState('problem');
    const [isRunning, setIsRunning] = useState(false);
    const [output, setOutput] = useState(null);

    const handleRun = () => {
        setIsRunning(true);
        setOutput(null);
        setTimeout(() => {
            setIsRunning(false);
            setOutput({ status: 'success', message: 'Test Cases Passed: 3/3' });
        }, 1500);
    };

    return (
        <AnimatedPage className="h-screen flex flex-col md:flex-row overflow-hidden bg-background">
            {/* Left Panel: Problem Statement */}
            <div className="w-full md:w-1/2 flex flex-col h-full border-r border-white/5 bg-surface/50 backdrop-blur-sm">
                <div className="p-6 border-b border-white/5 flex justify-between items-center">
                    <h1 className="text-xl font-bold font-mono text-primary">Mission: Binary Siege</h1>
                    <span className="px-3 py-1 bg-white/5 rounded-full text-xs font-mono text-text-muted">ID: #8492</span>
                </div>

                <div className="flex-1 overflow-y-auto p-8 space-y-8 custom-scrollbar">
                    <section>
                        <h2 className="text-lg font-bold text-white mb-4">Description</h2>
                        <p className="text-text-muted leading-relaxed">
                            Your algorithm must traverse a binary tree and locate the node with the highest value path. The path must not contain consecutive negative values.
                            <br /><br />
                            Input will be provided as a JSON representation of the tree structure.
                        </p>
                    </section>

                    <section className="space-y-4">
                        <h2 className="text-lg font-bold text-white">I/O Format</h2>
                        <GlassCard className="p-4 bg-black/40 font-mono text-sm">
                            <p className="text-secondary mb-2">// Input</p>
                            <p className="text-text-muted">{'root = [1, 2, 3, null, 5]'}</p>
                            <br />
                            <p className="text-primary mb-2">// Output</p>
                            <p className="text-text-muted">15</p>
                        </GlassCard>
                    </section>

                    <section>
                        <h2 className="text-lg font-bold text-white mb-4">Constraints</h2>
                        <ul className="list-disc list-inside text-text-muted space-y-2">
                            <li>Time Limit: 2.0s</li>
                            <li>Memory Limit: 256MB</li>
                            <li>0 {'<='} Nodes {'<='} 10^4</li>
                        </ul>
                    </section>
                </div>
            </div>

            {/* Right Panel: Editor */}
            <div className="w-full md:w-1/2 flex flex-col h-full relative">
                <div className="flex items-center justify-between p-2 bg-[#1e1e1e] border-b border-white/5">
                    <div className="flex gap-2">
                        <button className="px-4 py-2 bg-white/5 rounded text-xs font-mono text-text-main hover:bg-white/10 transition-colors">main.js</button>
                    </div>
                </div>

                <div className="flex-1 relative">
                    <Editor
                        height="100%"
                        defaultLanguage="javascript"
                        theme="vs-dark"
                        value={code}
                        onChange={(value) => setCode(value)}
                        options={{
                            minimap: { enabled: false },
                            fontSize: 14,
                            fontFamily: 'JetBrains Mono',
                            padding: { top: 20 },
                            scrollBeyondLastLine: false,
                        }}
                    />
                </div>

                {/* Output Console (Overlay) */}
                {output && (
                    <motion.div
                        initial={{ y: 100 }}
                        animate={{ y: 0 }}
                        className={`absolute bottom-16 left-4 right-4 p-4 rounded-lg border backdrop-blur-md ${output.status === 'success' ? 'bg-success/10 border-success/30' : 'bg-error/10 border-error/30'}`}
                    >
                        <div className="flex items-start gap-3">
                            {output.status === 'success' ? <CheckCircle className="text-success h-5 w-5" /> : <AlertCircle className="text-error h-5 w-5" />}
                            <div>
                                <h4 className={`font-bold text-sm ${output.status === 'success' ? 'text-success' : 'text-error'}`}>
                                    {output.status === 'success' ? 'Execution Successful' : 'Runtime Error'}
                                </h4>
                                <p className="text-text-muted text-xs font-mono mt-1">{output.message}</p>
                            </div>
                        </div>
                    </motion.div>
                )}

                {/* Action Bar */}
                <div className="p-4 bg-surface border-t border-white/5 flex justify-end gap-4">
                    <NeonButton variant="ghost" size="sm" onClick={() => setOutput(null)}>
                        <RefreshCcw className="mr-2 h-4 w-4" /> Reset
                    </NeonButton>
                    <NeonButton variant="primary" size="md" onClick={handleRun} isLoading={isRunning}>
                        <Play className="mr-2 h-4 w-4" /> Run Code
                    </NeonButton>
                </div>
            </div>
        </AnimatedPage>
    );
};

export default ChallengePage;
