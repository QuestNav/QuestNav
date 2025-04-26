const fs = require('fs');
const path = require('path');

/**
 * Custom Docusaurus plugin that reads CSV files at build time
 * and converts them to JavaScript data
 */
module.exports = function awardsCSVLoader(context, options) {
    return {
        name: 'awards-csv-loader',

        // This lifecycle hook runs at build time
        async loadContent() {
            const { siteDir } = context;

            // Paths to the CSV files in the static/data directory
            const awardsPath = path.join(siteDir, 'static', 'data', 'awards.csv');

            // Read and parse CSV files
            const awardsData = parseCSV(fs.readFileSync(awardsPath, 'utf8'));

            return {
                awardsData,
            };
        },

        // This hook makes the data available as a global variable
        async contentLoaded({ content, actions }) {
            const { setGlobalData } = actions;

            // Make the data available globally
            setGlobalData({
                awardsData: content.awardsData,
            });
        }
    };
};

/**
 * Helper function to parse CSV data
 */
function parseCSV(csvText) {
    const lines = csvText.split('\n');
    const headers = lines[0].split(',').map(header => header.trim());

    return lines.slice(1)
        .filter(line => line.trim().length > 0)
        .map(line => {
            const values = line.split(',').map(value => value.trim());
            const entry = {};

            headers.forEach((header, index) => {
                entry[header] = values[index] || '';
            });

            return entry;
        });
}