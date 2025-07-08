import { useEffect, useRef, useState } from "react";

type RollingNumberProps = {
  value: number;
  duration?: number; // ms, default 250
};

export function RollingNumber({ value, duration = 400 }: RollingNumberProps) {
  const [displayed, setDisplayed] = useState(value);
  const rafId = useRef<number | null>(null);

  const easeOut = (t: number, pow: number = 6): number => 1 - Math.pow(1 - t, pow);

  // Two-phase curve: first part linear, final ≈100 ms ease-out
  const twoPhase = (t: number, linearFraction: number): number => {
    if (t <= linearFraction) return t; // linear segment
    const rest = (t - linearFraction) / (1 - linearFraction); // 0-1
    return linearFraction + easeOut(rest) * (1 - linearFraction);
  };

  useEffect(() => {
    if (displayed === value) return;

    const start = displayed;
    const end = value;
    const settleMs = 100; // ease out threshold
    const linearFraction = duration <= settleMs ? 0 : (duration - settleMs) / duration;
    const startTime = performance.now();

    const animate = (now: number) => {
      const elapsed = now - startTime;
      const progress = Math.min(elapsed / duration, 1);          // 0-1
      const eased = twoPhase(progress, linearFraction);          // curve
      const current = start + (end - start) * eased;

      setDisplayed(Math.round(current));

      if (progress < 1) {
        rafId.current = requestAnimationFrame(animate);
      }
    };

    rafId.current = requestAnimationFrame(animate);

    // Proper cleanup function
    return () => {
      if (rafId.current !== null) {
        cancelAnimationFrame(rafId.current);
        rafId.current = null;
      }
    };
    // Only run when value changes
    // eslint-disable-next-line
  }, [value]); 

  return (
    <span>{displayed}</span>
  );
}