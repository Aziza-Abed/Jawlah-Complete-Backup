import React from 'react';

interface GlassCardProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode;
  variant?: 'default' | 'hover' | 'panel';
  noPadding?: boolean;
}

export default function GlassCard({ 
  children, 
  className = '', 
  variant = 'default',
  noPadding = false,
  ...props 
}: GlassCardProps) {
  
  const baseStyles = 'glass transition-all duration-300 border border-white/10';
  
  const variants = {
    default: 'rounded-xl bg-white/[0.03]',
    hover: 'rounded-xl bg-white/[0.03] hover:bg-white/[0.06] hover:-translate-y-1 hover:shadow-lg hover:shadow-primary/10 cursor-pointer',
    panel: 'rounded-2xl bg-surface/50 shadow-xl backdrop-blur-xl',
  };

  const paddingClass = noPadding ? '' : 'p-6';

  return (
    <div 
      className={`${baseStyles} ${variants[variant]} ${paddingClass} ${className}`}
      {...props}
    >
      {children}
    </div>
  );
}
