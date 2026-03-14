# QuestNav Config UI

A Vite + Vue 3 frontend used to configure the QuestNav Unity application.

## Prerequisites
- pnpm must be installed. Follow the official instructions: https://pnpm.io/installation

## Build
Run the following from this folder (the `unity/ui` directory):

```
pnpm install && pnpm build
```

This will produce the built UI files in `../Assets/StreamingAssets/ui` for Unity to consume.

## Optional (Development)
If you want to run the dev server locally:

```
pnpm install
pnpm dev
```

Then open the printed local URL in your browser. API requests are proxied to the Quest device as configured in `vite.config.ts`.
