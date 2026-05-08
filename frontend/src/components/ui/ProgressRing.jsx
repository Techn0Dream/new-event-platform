import React from 'react';
import { motion } from 'framer-motion';

const ProgressRing = ({ radius = 60, stroke = 8, progress = 0, color = "#06B6D4" }) => {
    const normalizedRadius = radius - stroke * 2;
    const circumference = normalizedRadius * 2 * Math.PI;
    const strokeDashoffset = circumference - (progress / 100) * circumference;

    return (
        <div className="relative flex items-center justify-center">
            <svg
                height={radius * 2}
                width={radius * 2}
                className="rotate-[-90deg]"
            >
                <circle
                    stroke="rgba(255,255,255,0.1)"
                    strokeWidth={stroke}
                    fill="transparent"
                    r={normalizedRadius}
                    cx={radius}
                    cy={radius}
                />
                <motion.circle
                    stroke={color}
                    strokeWidth={stroke}
                    strokeLinecap="round"
                    fill="transparent"
                    r={normalizedRadius}
                    cx={radius}
                    cy={radius}
                    style={{ strokeDasharray: circumference + ' ' + circumference }}
                    initial={{ strokeDashoffset: circumference }}
                    animate={{ strokeDashoffset }}
                    transition={{ duration: 1.5, ease: "easeInOut" }}
                />
            </svg>
            <div className="absolute flex flex-col items-center">
                <span className="text-2xl font-bold font-mono text-white">{progress}%</span>
            </div>
        </div>
    );
};

export default ProgressRing;
