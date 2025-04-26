import React, { useState, useEffect, useRef } from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import { usePluginData } from '@docusaurus/useGlobalData';

interface FeatureDetailItem {
    icon: string;
    text: string;
}

interface AwardItem {
    team_number: string;
    year: string;
    event_name: string;
    award_name: string;
}

interface FeatureData {
    title: string;
    subtitle: string;
    statValue: number;
    statSuffix?: string;
    statDescription: string;
    icon: string;
    colorStart: string;
    colorEnd: string;
    details: FeatureDetailItem[];
}

interface FeatureShowcaseProps {
    feature: FeatureData;
}

/**
 * FeatureShowcase component to display a single feature with animated stat
 */
const FeatureShowcase: React.FC<FeatureShowcaseProps> = ({ feature }) => {
    const [count, setCount] = useState(0);
    const [isVisible, setIsVisible] = useState(false);
    const [hasAnimated, setHasAnimated] = useState(false);
    const [isFinished, setIsFinished] = useState(false);
    const showcaseRef = useRef<HTMLDivElement>(null);
    const scrollTrackRef = useRef<HTMLDivElement>(null);
    const scrollContainerRef = useRef<HTMLDivElement>(null);

    // Set up intersection observer to detect when component is in view
    useEffect(() => {
        // Debounce timer to prevent flickering
        let debounceTimer: NodeJS.Timeout | null = null;

        const observer = new IntersectionObserver((entries) => {
            const [entry] = entries;

            // Clear any existing timer
            if (debounceTimer) {
                clearTimeout(debounceTimer);
            }

            // Set a new timer to update visibility state
            debounceTimer = setTimeout(() => {
                // Component is visible
                if (entry.isIntersecting) {
                    setIsVisible(true);
                    if (!hasAnimated) {
                        setHasAnimated(true);
                    }
                }
                // Component is no longer visible with a threshold buffer
                else if (entry.intersectionRatio < 0.05) {
                    setIsVisible(false);
                    // Reset isFinished when component is no longer visible
                    setIsFinished(false);
                }
            }, 100); // Small delay to prevent flickering
        }, {
            // Multiple thresholds for smoother transitions
            threshold: [0.05, 0.15, 0.5],
            // Add root margin to start transitions earlier
            rootMargin: '0px 0px -10% 0px'
        });

        if (showcaseRef.current) {
            observer.observe(showcaseRef.current);
        }

        return () => {
            if (showcaseRef.current && observer) {
                observer.unobserve(showcaseRef.current);
            }
            // Clear any remaining timers
            if (debounceTimer) {
                clearTimeout(debounceTimer);
            }
        };
    }, [hasAnimated]);

    // Setup scroll animation calculation
    useEffect(() => {
        if (!scrollTrackRef.current || !scrollContainerRef.current) return;

        // Fixed speed calculation
        const scrollTrack = scrollTrackRef.current;
        const itemCount = feature.details.length;

        // Base duration calculation on item count - this sets consistent speed
        // Normalize to a standard duration based on item count
        // 5 seconds per item, with a minimum of 20 seconds
        const baseDurationPerItem = 5; // seconds per item
        const minDuration = 20; // minimum duration in seconds
        const calculatedDuration = Math.max(itemCount * baseDurationPerItem, minDuration);

        // Apply the duration to the CSS variable
        scrollTrack.style.setProperty('--scroll-duration', `${calculatedDuration}s`);

        // Ensure track has the animation class
        setTimeout(() => {
            if (scrollTrackRef.current) {
                scrollTrackRef.current.classList.add(styles['scroll-animation']);
            }
        }, 100);
    }, [feature.details.length]);

    // Handle counter animation when component becomes visible
    useEffect(() => {
        if (!isVisible || !hasAnimated) return;

        // Reset count and isFinished state when becoming visible again
        if (!isVisible && isFinished) {
            setCount(0);
            setIsFinished(false);
        }

        const targetValue = feature.statValue;

        // Animate the counters
        const duration = 2000; // 2 seconds duration
        const frameRate = 30; // Updates per second
        const totalFrames = duration / (1000 / frameRate);

        let frame = 0;

        const animationInterval = setInterval(() => {
            frame++;

            if (frame <= totalFrames) {
                // Use easeOutExpo for dramatic start and smoother finish
                const progress = frame < totalFrames / 2
                    ? 4 * Math.pow(frame / totalFrames, 3)
                    : 1 - Math.pow(-2 * frame / totalFrames + 2, 3) / 2;

                setCount(Math.floor(targetValue * progress));
            } else {
                // Ensure we end up with exact target values
                setCount(targetValue);
                // Set isFinished to true when animation completes
                setIsFinished(true);
                clearInterval(animationInterval);
            }
        }, 1000 / frameRate);

        return () => clearInterval(animationInterval);
    }, [isVisible, hasAnimated, feature.statValue]);

    // Reset animation state when component becomes invisible
    useEffect(() => {
        if (!isVisible) {
            // Reset count to 0 when component is not visible
            setCount(0);
        }
    }, [isVisible]);

    // Create detail items
    const detailItems = feature.details.map((detail, i) => (
        <div key={`detail-${i}`} className={styles.featureDetailItem}>
            <div className={styles.featureDetailIcon}>{detail.icon}</div>
            <div className={styles.featureDetailContent}>
                <span className={styles.featureDetailText}>{detail.text}</span>
            </div>
        </div>
    ));

    return (
        <div className={styles.featureShowcaseWrapper}>
            <div
                ref={showcaseRef}
                className={clsx(
                    styles.featureShowcaseContainer,
                    isVisible && styles.visible
                )}
                style={{ background: `linear-gradient(135deg, ${feature.colorStart}, ${feature.colorEnd})` }}
            >
                {/* Diagonal background lines */}
                <div className={styles.diagonalLines}></div>

                <div className={styles.featureHeader}>
                    <div className={styles.featureIconCircle}>
                        <span className={styles.featureIcon}>{feature.icon}</span>
                    </div>
                    <h3 className={styles.featureTitle}>{feature.title}</h3>
                    <div className={styles.featureSubtitle}>{feature.subtitle}</div>
                </div>

                <div className={styles.featureStatsContainer}>
                    <div className={styles.featureStatBox}>
                        <div className={clsx(
                            styles.statValue,
                            // Only apply milestone class if count reaches the final value,
                            // the component is currently visible and animation is finished
                            (count === feature.statValue) && isVisible && isFinished && styles.milestone
                        )}>
                            <span className={styles.statCount}>{count}</span>
                            {feature.statSuffix && (
                                <span className={styles.statSuffix}>{feature.statSuffix}</span>
                            )}
                        </div>
                        <div className={styles.statDescription}>{feature.statDescription}</div>

                        {/* Dynamic shadow effect */}
                        <div className={clsx(styles.shadowAnimator, styles.featureShadow)}></div>

                        {/* Scrolling details container */}
                        <div ref={scrollContainerRef} className={styles.scrollingContainer}>
                            <div ref={scrollTrackRef} className={styles.scrollTrack}>
                                {detailItems}
                                {detailItems} {/* Duplicate for continuous scroll */}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

interface FeaturesGridProps {
    /**
     * Optional className for additional styling
     */
    className?: string;
}

/**
 * FeaturesGrid component that displays all features in a responsive grid
 */
const FeaturesGrid: React.FC<FeaturesGridProps> = ({
                                                       className
                                                   }) => {
    // Get data from Docusaurus context
    const {siteConfig} = useDocusaurusContext();
    const buildTime = siteConfig.customFields?.buildTime ||
        new Date().toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });

    // Get award data from the plugin
    const { awardsData } = usePluginData('awards-csv-loader') as {
        awardsData: AwardItem[];
    };

    // Get GitHub releases data
    const { releasesData } = usePluginData('github-releases-loader') as {
        releasesData: {
            totalDownloads: number;
            recentVersionsCount: number;
        }
    };

    // Convert award data to feature detail items
    const awardFeatureDetails: FeatureDetailItem[] = awardsData.map(award => ({
        icon: "🏆",
        text: `${award.award_name}\n${award.event_name}\n${award.year} | Team ${award.team_number}`
    }));

    // Data for the features
    const features: FeatureData[] = [
        {
            title: "Lightning Fast",
            subtitle: "Real-time position tracking",
            statValue: 120,
            statSuffix: "Hz",
            statDescription: "Localization updates per second",
            icon: "⚡",
            colorStart: "var(--ifm-color-primary-darkest)",
            colorEnd: "var(--ifm-color-primary-darker)",
            details: [
                { icon: "📊", text: "Position updates faster than possible before" },
                { icon: "🔄", text: "Runs faster than your RoboRIO" },
                { icon: "⏱️", text: "Ultra-low latency tracking" },
                { icon: "📱", text: "Enables smooth real-time path following" }
            ]
        },
        {
            title: "Easy Setup",
            subtitle: "Up and running in no time",
            statValue: 30,
            statSuffix: "min",
            statDescription: "From unboxing to operation",
            icon: "🚀",
            colorStart: "var(--ifm-color-teal-dark)",
            colorEnd: "var(--ifm-color-teal)",
            details: [
                { icon: "📦", text: "Buy a headset" },
                { icon: "💻", text: "Deploy the app" },
                { icon: "✏️", text: "Set your Team Number" },
                { icon: "📄", text: "Copy a Java file" },
                { icon: "✅", text: "Ready to navigate" }
            ]
        },
        {
            title: "Constant Growth",
            subtitle: "Regular improvements & updates",
            statValue: releasesData.recentVersionsCount || 10,
            statSuffix: "ver",
            statDescription: "Released in last 3 months",
            icon: "📈",
            colorStart: "var--ifm-color-primary-darkest)",
            colorEnd: "var(--ifm-color-primary-darker)",
            details: [
                { icon: "🔧", text: "Frequent bug fixes" },
                { icon: "✨", text: "New features monthly" },
                { icon: "🧠", text: "Dedicated Developers" },
                { icon: "🤝", text: "Community-driven development" }
            ]
        },
        {
            title: "Open To All",
            subtitle: "Free & open source software",
            statValue: releasesData.totalDownloads || 125,
            statSuffix: "DLs",
            statDescription: "Latest version downloads",
            icon: "🌐",
            colorStart: "var(--ifm-color-teal-dark)",
            colorEnd: "var(--ifm-color-teal)",
            details: [
                { icon: "💸", text: "Zero cost to teams" },
                { icon: "🔓", text: "MIT licensed code" },
                { icon: "🔍", text: "Transparent development" },
                { icon: "🤖", text: "FRC community supported" }
            ]
        },
        {
            title: "Ultra Reliable",
            subtitle: "Precise positioning at all times",
            statValue: 1,
            statSuffix: "cm",
            statDescription: "Positioning precision",
            icon: "🎯",
            colorStart: "var(--ifm-color-primary-darkest)",
            colorEnd: "var(--ifm-color-primary-darker)",
            details: [
                { icon: "🔄", text: "AprilTag + VIO fusion" },
                { icon: "👁️", text: "Works even when tags are hidden*" },
                { icon: "📏", text: "Sub-centimeter accuracy" },
                { icon: "🧭", text: "Multi-sensor orientation tracking" }
            ]
        },
        {
            title: "Built to Win",
            subtitle: "Powering FIRST Robotics Champions",
            statValue: awardsData.length,
            statDescription: "Total awards achieved with QuestNav",
            icon: "🏆",
            colorStart: "var(--ifm-color-teal)",
            colorEnd: "var(--ifm-color-teal-dark)",
            details: awardFeatureDetails
        }
    ];

    return (
        <section className={clsx(styles.featuresSection, className)}>
            <div className={styles.featuresContainer}>
                <h2 className={styles.featuresHeading}>Why Choose QuestNav?</h2>
                <div className={styles.featuresGrid}>
                    {features.map((feature, index) => (
                        <FeatureShowcase
                            key={`feature-${index}`}
                            feature={feature}
                        />
                    ))}
                </div>
                <div className={styles.featureNoteContainer}>
                    <div className={styles.featureNote}>* AprilTag compatibility coming soon</div>
                    <div className={styles.timestampPill}>
                        <div className={styles.liveIndicator}></div>
                        <span>Last updated: {buildTime}</span>
                    </div>
                </div>
            </div>
        </section>
    );
};

export default FeaturesGrid;