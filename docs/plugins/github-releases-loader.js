// @ts-check
import axios from "axios";

/**
 * GitHub Releases Plugin for Docusaurus
 *
 * This plugin fetches release data from GitHub API at build time,
 * calculates download statistics for assets published in the last 3 months,
 * and makes them available as global data to use in components.
 *
 * @param {import('@docusaurus/types').LoadContext} context
 * @param {Object} options
 */
async function githubReleasesPlugin(context, options) {
    const {
        repository = 'QuestNav/QuestNav',
        monthsToCheck = 3,
        defaultCount = 0, // Default value if API fails
    } = options || {};

    return {
        name: 'github-releases-loader',

        async loadContent() {
            try {
                console.log(`Fetching GitHub releases data for ${repository}...`);

                // Calculate date 3 months ago for filtering
                const now = new Date();
                const threeMonthsAgo = new Date();
                threeMonthsAgo.setMonth(now.getMonth() - monthsToCheck);

                // Fetch releases from GitHub API (public repo, no auth needed)
                const response = await axios.get(
                    `https://api.github.com/repos/${repository}/releases`
                );

                // Process release data
                let totalDownloads = 0;
                let recentVersionsCount = 0;
                const recentReleases = [];

                if (response.data && Array.isArray(response.data)) {
                    // Filter releases from the last 3 months
                    const recentReleasesData = response.data.filter(release => {
                        const releaseDate = new Date(release.published_at);
                        return releaseDate >= threeMonthsAgo;
                    });

                    recentVersionsCount = recentReleasesData.length;

                    // Calculate total downloads from all assets in recent releases
                    recentReleasesData.forEach(release => {
                        if (release.assets && Array.isArray(release.assets)) {
                            const releaseDownloads = release.assets.reduce(
                                (sum, asset) => sum + asset.download_count,
                                0
                            );

                            totalDownloads += releaseDownloads;

                            recentReleases.push({
                                name: release.name || release.tag_name,
                                date: release.published_at,
                                downloads: releaseDownloads,
                                version: release.tag_name,
                                prerelease: release.prerelease,
                            });
                        }
                    });
                }

                console.log(`Found ${recentVersionsCount} releases in the last ${monthsToCheck} months`);
                console.log(`Total downloads: ${totalDownloads}`);

                return {
                    totalDownloads,
                    recentVersionsCount,
                    recentReleases,
                    fetchedAt: new Date().toISOString(),
                };
            } catch (error) {
                console.error('Error fetching GitHub releases:', error.message);

                // Return default values if API call fails
                return {
                    totalDownloads: defaultCount,
                    recentVersionsCount: 0,
                    recentReleases: [],
                    fetchedAt: new Date().toISOString(),
                    error: error.message,
                };
            }
        },

        async contentLoaded({ content, actions }) {
            const { setGlobalData } = actions;

            // Make the data available globally
            setGlobalData({
                releasesData: content,
            });
        },
    };
}

module.exports = githubReleasesPlugin;