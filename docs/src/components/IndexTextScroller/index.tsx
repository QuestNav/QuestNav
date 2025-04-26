import React, { useState, useEffect, JSX, useRef } from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';

// Props interface for the text scroller component
interface AnimatedTextProps {
    staticText: string;       // Text that remains fixed on the left side
    movingTextItems: string[]; // Array of strings to rotate through
    duration?: number;        // Animation cycle duration in ms
    className?: string;       // Optional CSS class
}

export default function IndexTextScroller({
                                              staticText,
                                              movingTextItems,
                                              duration = 2000,
                                              className,
                                          }: AnimatedTextProps): JSX.Element {
    // Track current item in the movingTextItems array
    const [currentIndex, setCurrentIndex] = useState(0);
    // Animation state flag to control CSS transitions
    const [isAnimating, setIsAnimating] = useState(false);
    // Dynamically track width of current text to prevent layout shifts
    const [width, setWidth] = useState(0);
    // Reference to the text element for measuring width
    const textRef = useRef<HTMLSpanElement>(null);

    // Measure and update width whenever the text changes
    useEffect(() => {
        if (textRef.current) {
            setWidth(textRef.current.offsetWidth);
        }
    }, [movingTextItems[currentIndex]]);

    // Handle animation cycle and text rotation
    useEffect(() => {
        if (movingTextItems.length <= 1) return;

        // Start animation after specified duration
        const animationTimeout = setTimeout(() => {
            setIsAnimating(true);

            // After animation completes, change to next text
            const textChangeTimeout = setTimeout(() => {
                setCurrentIndex((prevIndex) => (prevIndex + 1) % movingTextItems.length);
                setIsAnimating(false);
            }, duration / 3); // Text changes 1/3 through animation cycle

            return () => clearTimeout(textChangeTimeout);
        }, duration);

        return () => clearTimeout(animationTimeout);
    }, [currentIndex, duration, movingTextItems]);

    return (
        <div className={clsx(styles.container, className)}>
            <div className={styles.textWrapper}>
                {/* Static text displayed on the left */}
                <span className={styles.staticText}>{staticText}</span>
                {/* Container with dynamic width to prevent layout shifts */}
                <div
                    className={styles.movingTextContainer}
                    style={{ width: `${width}px` }}
                >
                    {/* Text with animation applied based on state */}
                    <span
                        ref={textRef}
                        className={clsx(
                            styles.movingText,
                            isAnimating ? styles.slideOut : styles.slideInBounce
                        )}
                        style={{
                            animationDuration: isAnimating ? `${duration / 3}ms` : `${duration / 2}ms`,
                        }}
                    >
                        {movingTextItems[currentIndex]}
                    </span>
                </div>
            </div>
        </div>
    );
}