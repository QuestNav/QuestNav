import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)


const config: Config = {
  title: 'QuestNav',
  tagline: 'The next generation navigation solution',
  favicon: 'img/branding/QuestNavLogoSquare.svg',

  // Set the production url of your site here
  url: 'https://questnav.gg',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'QuestNav', // Usually your GitHub org/user name.
  projectName: 'QuestNav', // Usually your repo name.

  // Ignore broken links to keep CI builds clean
  // The /api/* paths are static files served separately from Docusaurus
  // and will be available at runtime after deployment
  onBrokenLinks: 'ignore',
  onBrokenMarkdownLinks: 'warn',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  customFields: {
    buildTime: new Date().toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }),
  },

  plugins: [
    require('./plugins/awards-csv-loader'),
    [
      require('./plugins/github-releases-loader'),
      {
        repository: 'QuestNav/QuestNav',
        monthsToCheck: 3,
        defaultCount: 1250 // Fallback if GitHub API fails
      }
    ]
  ],

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          // Please change this to your repo.
          // Remove this to remove the "edit this page" links.
          editUrl:
            'https://github.com/QuestNav/QuestNav/tree/main/docs/',
          versions: {
            current: {label: 'Development'},
          },
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    // Announcement bar for the rewrite, should be removed when the rewrite is done
    announcementBar: {
      id: 'version_notification',
      content: '⚠️ A major rewrite is in progress for the 2026 season! <a href="/docs/rewrite">Learn more</a>',
      backgroundColor: 'var(--ifm-background-color)',
      textColor: 'var(--ifm-color-primary)',
      isCloseable: false,
    },
    // Replace with your project's social card
    navbar: {
      title: '',
      logo: {
        alt: 'Logo',
        src: 'img/branding/QuestNavLogo.svg',
        srcDark: 'img/branding/QuestNavLogo-Dark.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docsSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          type: 'dropdown',
          label: 'API Reference',
          position: 'left',
          items: [
            {
              label: 'Protocol Buffers',
              href: 'https://questnav.gg/static/api/proto/',
            },
            {
              label: 'Java API',
              href: 'https://questnav.gg/static/api/java/',
            },
            {
              label: 'C# API',
              href: 'https://questnav.gg/static/api/csharp/',
            },
          ],
        },
        {
          to: 'https://github.com/QuestNav/QuestNav/releases',
          label: 'Releases',
          position: 'left',
        },
        {
          type: 'docsVersionDropdown',
          position: 'right',
        },
        {
          href: 'https://github.com/QuestNav/QuestNav',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Getting Started',
              to: '/docs/getting-started/about',
            },
            {
              label: 'Development Guide',
              to: '/docs/development/development-setup',
            },
          ],
        },
        {
          title: 'API Reference',
          items: [
            {
              label: 'Protocol Buffers',
              href: 'https://questnav.gg/static/api/proto/',
            },
            {
              label: 'Java API',
              href: 'https://questnav.gg/static/api/java/',
            },
            {
              label: 'C# API',
              href: 'https://questnav.gg/static/api/csharp/',
            },
          ],
        },
        {
          title: 'Community',
          items: [
            {
              label: 'Discord',
              href: 'https://discord.gg/hD3FtR7YAZ',
            }
          ],
        },
        {
          title: 'Need Help?',
          items: [
            {
              label: 'Report Issues',
              href: 'https://github.com/QuestNav/QuestNav/issues',
            },
            {
              label: 'GitHub Discussions',
              href: 'https://github.com/QuestNav/QuestNav/discussions',
            },
            {
              label: 'Contributing Guide',
              to: '/docs/development/contributing',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/QuestNav/QuestNav',
            },
          ],
        },
      ],
      copyright: `Licensed under the MIT License. Copyright © ${new Date().getFullYear()} QuestNav.`,
    },
    prism: {
      theme: prismThemes.vsLight,
      darkTheme: prismThemes.vsDark,
      additionalLanguages: ['java']
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
