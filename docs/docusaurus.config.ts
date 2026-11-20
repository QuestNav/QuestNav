import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';
import * as fs from 'fs';
import * as path from 'path';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

const versions: string[] = JSON.parse(
  fs.readFileSync(path.resolve(__dirname, 'versions.json'), 'utf-8')
);
const latestVersion = versions[0];

// Newest QuestNavLib release for 2026 (roboRIO) robots. Update it when a new 2026 lib is published.
const questnavLib2026Version = '2026-3.0.0-beta';

// Mirrors the repository choice in questnav-lib/build.gradle: plain, dev, snapshot and pr
// versions are published to /snapshots, prereleases such as -beta and -rc to /releases.
function questnavLibJsonUrl(version: string): string {
  const versionType = version.split('-').slice(2).join('-');
  const isSnapshot =
    ['', 'dev', 'snapshot', 'pr'].includes(versionType) || version.includes('-SNAPSHOT');
  const repo = isSnapshot ? 'snapshots' : 'releases';
  return `https://maven.questnav.gg/${repo}/gg/questnav/questnavlib-json/${version}/questnavlib-json-${version}.json`;
}

const config: Config = {
  title: 'QuestNav',
  tagline: 'The next generation navigation solution',
  favicon: 'img/branding/QuestNavLogoSquare.svg',

  url: 'https://questnav.gg',
  baseUrl: '/',

  organizationName: 'QuestNav',
  projectName: 'QuestNav',

  // Generated API docs under /static/api/ are added by CI after the build; link them with pathname:///
  onBrokenLinks: 'throw',

  markdown: {
    mermaid: true,
    hooks: {
      onBrokenMarkdownLinks: 'warn',
    },
  },

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
    // Vendordep for the latest versioned docs
    questnavLibJsonUrl: questnavLibJsonUrl(latestVersion),
    questnavLib2026Version,
    questnavLib2026JsonUrl: questnavLibJsonUrl(questnavLib2026Version),
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

  themes: ['@docusaurus/theme-mermaid'],

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
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
              href: 'https://discord.gg/Zfan2qgkRZ',
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
