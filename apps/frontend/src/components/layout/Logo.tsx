interface LogoProps {
  className?: string;
}

export function Logo({ className = '' }: LogoProps) {
  return (
    <div className={`flex items-center ${className}`}>
      <span className="text-base font-bold tracking-tight">
        <span className="text-ink">Light</span>
        <span className="mx-1 text-accent-green">&amp;</span>
        <span className="text-accent-blue">Vibe</span>
      </span>
    </div>
  );
}
