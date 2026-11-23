# Project instructions for Codex

This repository is an early demonstration of **Nébula Anime**, an original anime discovery portal.

## Current state
- The working demo is a single static `index.html`, with inline CSS and JS.
- Data is entirely fictitious. Player pages are placeholders; no video streaming is configured.
- Advertising slots are placeholders and no ad network is connected.
- GitHub Pages deployment workflow is in `.github/workflows/deploy.yml`.

## Product principles
- Aim for polished, mobile-first UI, fast load times, accessible interactions and original identity.
- Do not copy site code, assets, branding or videos from AnimeAV1 or any other third party.
- Do not add unauthorized anime streaming, pirated embeds, scraping of restricted sources, circumvention of licensing, or deceptive ad flows.
- Use only owned, licensed, public-domain or otherwise authorized media, with attribution where necessary.
- Do not imply a site is approved by AdSense or that ad earnings are guaranteed.
- Any future ad network integration requires policy review, consent where necessary and approval by the owner.

## Roadmap
1. Improve demo usability, responsive navigation, favorites, search and error handling.
2. Refactor into an Angular application with SSR/prerendering for SEO.
3. Add ASP.NET Core API and PostgreSQL for authorized metadata and administration.
4. Build proper privacy, consent and content rights workflows.
5. Integrate licensed content sources; add paid hosting only after owner approval.
6. Set up deployment with automated checks and operational monitoring.

## Working conventions
- Keep changes small, document test instructions, and use pull requests.
- Never put API keys or credentials into the public repository.
- For each change, report what was implemented and what remains unverified.
