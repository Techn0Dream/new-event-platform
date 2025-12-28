import React from 'react';
import { cn } from '../../utils/cn';

const AnimatedInput = React.forwardRef(({ label, error, className, ...props }, ref) => {
    return (
        <div className="space-y-1">
            {label && <label className="text-xs font-mono text-text-muted uppercase tracking-wider pl-1">{label}</label>}
            <div className="relative group">
                <input
                    ref={ref}
                    className={cn(
                        "w-full bg-white/5 border border-white/10 rounded-lg px-4 py-3 text-text-main placeholder:text-text-muted/50 focus:outline-none focus:border-primary/50 transition-all duration-300",
                        "focus:shadow-[0_0_15px_rgba(6,182,212,0.15)]",
                        error && "border-error/50 focus:border-error/80 focus:shadow-[0_0_15px_rgba(239,68,68,0.15)]",
                        className
                    )}
                    {...props}
                />
                <div className="absolute inset-0 rounded-lg bg-gradient-to-r from-primary/20 to-secondary/20 opacity-0 group-hover:opacity-100 pointer-events-none transition-opacity duration-500 -z-10 blur-md" />
            </div>
            {error && <p className="text-xs text-error font-mono pl-1">{error}</p>}
        </div>
    );
});

AnimatedInput.displayName = "AnimatedInput";

export default AnimatedInput;
