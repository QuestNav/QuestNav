import type {ReactNode} from 'react';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';

import styles from './index.module.css';
import IndexTextScroller from "@site/src/components/IndexTextScroller";
import IndexBackgroundSlideshow from '@site/src/components/IndexBackgroundSlideshow';
import FeaturesGrid from "@site/src/components/FeaturesGrid";

// Function to get all media files from a directory
function getMediaFiles() {
    const images: string[] = [];
    const videos: string[] = [];

    // Base path to the static directory
    const basePath = '/img/index-slideshow/';

    // Example files - replace these with your actual files
    const exampleImages = [
        'QuestNav1732.webp',
        'QuestNav5152CloseUp.webp',
        'QuestNav2471.webp',
        'QuestNav4451Mid.webp'
    ];

    const exampleVideos = [
    ];

    // Add the base path to each filename
    exampleImages.forEach(file => {
        images.push(`${basePath}${file}`);
    });

    exampleVideos.forEach(file => {
        videos.push(`${basePath}${file}`);
    });

    return { images, videos };
}

function HomepageHeader() {
    const {siteConfig} = useDocusaurusContext();
    const { images, videos } = getMediaFiles();

    return (
        <header className={styles.heroContainer}>
            {/* Background Slideshow */}
            <IndexBackgroundSlideshow
                imageFiles={images}
                videoFiles={videos}
                duration={5000} // 5 seconds per image
            />


        <img className={styles.heroTitle} src={"img/branding/QuestNavLogo-Dark.svg"} width={'65%'} alt={'QuestNav'} />

            <div className={styles.textScrollerWrapper}>
                <IndexTextScroller
                    staticText="QuestNav is"
                    movingTextItems={["a winner.", "fast.", "easy to use.", "new.", "free.", "reliable."]}
                    duration={2000}
                />
            </div>

            <p className={styles.heroSubtitle}>
                {siteConfig.tagline}
            </p>

            <Link
                className={styles.ctaButton}
                to="/docs/getting-started/about">
                Get Started
            </Link>
        </header>
    );
}

export default function Home(): ReactNode {
    const {siteConfig} = useDocusaurusContext();
    return (
        <Layout
            title={`Welcome to ${siteConfig.title}`}
            description="QuestNav - The next generation navigation solution">
            <main>
                <HomepageHeader />
                <FeaturesGrid />
            </main>
        </Layout>
    );
}