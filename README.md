# The license plate game

This is a [Next.js](https://nextjs.org/) project bootstrapped with [`create-next-app`](https://github.com/vercel/next.js/tree/canary/packages/create-next-app).

## Getting Started

Disable next [telemetry](https://nextjs.org/telemetry#how-do-i-opt-out)

First, run the development server:

```bash
npm run dev
# or
yarn dev
# or
pnpm dev
# or
bun dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

You can start editing the page by modifying `app/page.tsx`. The page auto-updates as you edit the file.

This project uses [`next/font`](https://nextjs.org/docs/basic-features/font-optimization) to automatically optimize and load Inter, a custom Google Font.

## Learn More

To learn more about Next.js, take a look at the following resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.

You can check out [the Next.js GitHub repository](https://github.com/vercel/next.js/) - your feedback and contributions are welcome!

## Deploy on Vercel

The easiest way to deploy your Next.js app is to use the [Vercel Platform](https://vercel.com/new?utm_medium=default-template&filter=next.js&utm_source=create-next-app&utm_campaign=create-next-app-readme) from the creators of Next.js.

Check out our [Next.js deployment documentation](https://nextjs.org/docs/deployment) for more details.


## TODO
#### Functional game
- [x] Able to check off spotted plates.
- [x] Finish game
#### Basic CSS
- [ ] Tailwind CSS
#### Azure deployment.
- [ ] TBD - Web App or ACA (will include TLS and custom domain)
#### PWA
- [ ] Allow Android app installation.
- [ ] Offline gameplay.
#### Clean UI
- [x] US Map with terriotires lighting up.
- [x] Restructure site - home: active game + history, history page: historical map
- [x] Nicer plate picker.
- [x] Previous game stats.
- [x] License plate pictures.
- [x] Game state management
- [ ] Cleanup history. Show readonly map.
- [ ] Show list of selected state abbreviations in picker
#### Backend
- [ ] .Net 8+.
- [ ] (No)SQL persistence.
- [ ] Google auth (oauth-proxy or easy-auth)
- [ ] Online/offline sync.
#### Multiuser support
- [ ] User invite and onboarding
- [ ] Add user to game
- [ ] SignalR.