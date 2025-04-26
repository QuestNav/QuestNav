import React, { useState, useEffect, useRef } from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';

interface SlideshowProps {
    imageFiles: string[];
    videoFiles: string[];
    duration?: number;
}

const IndexBackgroundSlideshow: React.FC<SlideshowProps> = ({
                                                           imageFiles,
                                                           videoFiles,
                                                           duration = 5000
                                                       }) => {
    const [currentSlide, setCurrentSlide] = useState(0);
    const [activeMediaType, setActiveMediaType] = useState<'image' | 'video'>('image');
    const [mediaFiles, setMediaFiles] = useState<{type: 'image' | 'video', src: string}[]>([]);
    const videoRefs = useRef<HTMLVideoElement[]>([]);

    // Combine image and video files into a single array
    useEffect(() => {
        const images = imageFiles.map(src => ({ type: 'image' as const, src }));
        const videos = videoFiles.map(src => ({ type: 'video' as const, src }));

        // Mix images and videos together
        const allMedia = [...images, ...videos].sort(() => Math.random() - 0.5);
        setMediaFiles(allMedia);
    }, [imageFiles, videoFiles]);

    // Handle slide transitions
    useEffect(() => {
        if (mediaFiles.length === 0) return;

        const currentMedia = mediaFiles[currentSlide];
        setActiveMediaType(currentMedia.type);

        // If the current slide is a video, play it
        if (currentMedia.type === 'video' && videoRefs.current[currentSlide]) {
            const videoElement = videoRefs.current[currentSlide];
            if (videoElement) {
                videoElement.currentTime = 0;
                const playPromise = videoElement.play();

                if (playPromise !== undefined) {
                    playPromise.catch(error => {
                        console.error("Video playback error:", error);
                    });
                }
            }
        }

        // Set up the timer for the next slide
        const timer = setTimeout(() => {
            setCurrentSlide((prevSlide) => (prevSlide + 1) % mediaFiles.length);
        }, currentMedia.type === 'video' ? 0 : duration); // For videos, we'll use the onended event

        return () => clearTimeout(timer);
    }, [currentSlide, mediaFiles, duration]);

    // Handle video ended event
    const handleVideoEnded = () => {
        setCurrentSlide((prevSlide) => (prevSlide + 1) % mediaFiles.length);
    };

    if (mediaFiles.length === 0) {
        return null;
    }

    return (
        <div className={styles.slideshowBackground}>
            {mediaFiles.map((media, index) => {
                if (media.type === 'image') {
                    return (
                        <div
                            key={`image-${index}`}
                            className={clsx(
                                styles.slide,
                                currentSlide === index && activeMediaType === 'image' && styles.slideActive
                            )}
                            style={{ backgroundImage: `url(${media.src})` }}
                        />
                    );
                } else {
                    return (
                        <video
                            key={`video-${index}`}
                            ref={el => {
                                if (el) videoRefs.current[index] = el;
                            }}
                            className={clsx(
                                styles.videoSlide,
                                currentSlide === index && activeMediaType === 'video' && styles.videoSlideActive
                            )}
                            src={media.src}
                            muted
                            playsInline
                            onEnded={handleVideoEnded}
                        />
                    );
                }
            })}
            <div className={styles.slideOverlay} />
        </div>
    );
};

export default IndexBackgroundSlideshow;