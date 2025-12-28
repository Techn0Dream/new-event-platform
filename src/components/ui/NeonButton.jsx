import React from 'react';
import { motion } from 'framer-motion';
import { cn } from '../../utils/cn';
import { Loader2 } from 'lucide-react';

const NeonButton = ({
    children,
    variant = 'primary',
    size = 'md',
    className,
    isLoading,
    disabled,
    ...props
}) => {

    const variants = {
        primary: "bg-primary/10 text-primary border border-primary/50 hover:bg-primary/20 hover:shadow-[0_0_20px_rgba(6,182,212,0.4)]",
        secondary: "bg-secondary/10 text-secondary border border-secondary/50 hover:bg-secondary/20 hover:shadow-[0_0_20px_rgba(139,92,246,0.4)]",
        ghost: "bg-transparent text-text-muted hover:text-white hover:bg-white/5",
        danger: "bg-red-500/10 text-red-500 border border-red-500/50 hover:bg-red-500/20"
    };

    const sizes = {
        sm: "h-8 px-4 text-xs tracking-wider",
        md: "h-12 px-6 text-sm tracking-wider",
        lg: "h-14 px-8 text-base tracking-widest uppercase font-bold",
    };

    return (
        <motion.button
            whileHover={{ scale: disabled || isLoading ? 1 : 1.02 }}
            whileTap={{ scale: disabled || isLoading ? 1 : 0.98 }}
            className={cn(
                "relative rounded-lg font-mono flex items-center justify-center transition-all duration-300",
                sizes[size],
                variants[variant],
                (disabled || isLoading) && "opacity-50 cursor-not-allowed",
                className
            )}
            disabled={disabled || isLoading}
            {...props}
        >
            {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {children}

            {/* Glow Overlay */}
            {!disabled && !isLoading && variant !== 'ghost' && (
                <div className="absolute inset-0 rounded-lg bg-current opacity-0 hover:opacity-10 transition-opacity" />
            )}
        </motion.button>
    );
};

export default NeonButton;
