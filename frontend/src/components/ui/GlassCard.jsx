import React from 'react';
import { motion } from 'framer-motion';
import { cn } from '../../utils/cn';

const GlassCard = ({ children, className, hoverEffect = false, ...props }) => {
    return (
        <motion.div
            className={cn(
                "bg-surface backdrop-blur-md border border-white/5 rounded-xl overflow-hidden",
                hoverEffect && "hover:border-primary/30 hover:shadow-[0_0_20px_rgba(6,182,212,0.1)] transition-all duration-300",
                className
            )}
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5 }}
            {...props}
        >
            {children}
        </motion.div>
    );
};

export default GlassCard;
