# QuestNav Docs

The [questnav.gg](https://questnav.gg) site, built with [Docusaurus](https://docusaurus.io/).

## Layout

- `docs/`: the current ("Development") docs, served under `/docs/next/`
- `versioned_docs/`, `versioned_sidebars/`, `versions.json`: docs frozen at each release; the first entry in `versions.json` is served under `/docs/`
- `src/`: the landing page and React components used by the docs
- `plugins/`: build-time loaders (awards CSV, GitHub release download counts)

## Local development

```
npm ci
npm run start
```

## Checks

```
npm run build      # fails on broken links
npm run typecheck
```

The generated Protocol Buffer, Java and C# API docs are not part of this build. CI adds them under
`/static/api/` when it publishes the site, so link to them as `pathname:///static/api/...`.

## Publishing

The release workflow builds the site and publishes it to GitHub Pages
(`.github/workflows/reusable-publish-docs.yml`). Don't use `npm run deploy`.
