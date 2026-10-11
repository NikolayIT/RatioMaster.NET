# ratiomaster.net

The website of RatioMaster.NET, hosted on Cloudflare Workers.

- `site/page.html` is the home page and `site/i18n/<code>.json` its texts in each of the 38 languages of the program (`src/languages.js` lists them). `scripts/build-site.mjs` turns them into one page per language in `dist/site/`: English at `/`, the others at `/fr/`, `/pt-br/`, `/zh-hans/`... with localized dates, `dir="rtl"` for Arabic and Hebrew, and `hreflang` links between them. Wrangler runs it before every deploy and `wrangler dev` (the `build` entry in `wrangler.jsonc`), and it refuses to build when a translation is missing a text, changes a tag or loses a number.
- `public/` holds the files served as they are (stylesheet, images, 404 page); the build copies them into `dist/site/`. Cloudflare serves `dist/site/` directly from its static assets storage; those requests never run the Worker.
- `src/index.js` is the Worker. It answers the update check that every installed copy performs (`GET /vc.php?v=NNNN`, older builds `GET /version.html`), logs each check to a D1 database, sends a visitor who opens `/` to their language (the language picked in the page's menu, remembered in the `lang` cookie, else the browser's `Accept-Language`, else English), and 301-redirects the old PHP page URLs to the new page.
- `src/admin.js` renders `/admin`, a password-protected page with the last 10,000 version checks by date, country, version and operating system.
- `migrations/` holds the D1 schema, applied with Wrangler.

## Local development

```
npm install
npm run db:migrate:local
npm run dev
```

Then:

```
curl "http://localhost:8787/vc.php?v=0100"        # prints the current version
curl -I http://localhost:8787/features.php        # 301 to /#features
```

## First-time setup on Cloudflare

1. `npx wrangler login`
2. `npm run db:create` and paste the printed `database_id` into `wrangler.jsonc`.
3. `npm run db:migrate`
4. `npx wrangler secret put ADMIN_PASSWORD` and type the password for `/admin`.
5. `npm run deploy` for the first deployment. Every later push to `master` deploys automatically once the Git integration is connected:
   Workers & Pages -> ratiomaster-net -> Settings -> Build -> connect the GitHub repo, root directory `web`, no build command, deploy command `npx wrangler deploy`.

## Cut-over from the old server

1. Workers & Pages -> ratiomaster-net -> Settings -> Domains & Routes -> add `ratiomaster.net` and `www.ratiomaster.net`. Cloudflare replaces the DNS-only A and CNAME records.
2. Keep SSL/TLS -> Edge Certificates -> "Always Use HTTPS" **off**. RatioMaster.NET 0.43 targets .NET Framework 4.0 and calls `http://ratiomaster.net/vc.php` over plain HTTP; on current Windows it cannot complete a TLS handshake, so a forced redirect would silently break the update check for every old install.
3. Verify from outside:

```
curl -sS "http://ratiomaster.net/vc.php?v=0100"     # must print the version, not a redirect
curl -sSI "http://ratiomaster.net/vc.php?v=0100"    # 200, text/plain
curl -sSI https://ratiomaster.net/                   # 200
```

4. Uncomment the `routes` block in `wrangler.jsonc` so the config matches the dashboard.

## Changing the texts

Edit the English text in `site/i18n/en.json` (and the page structure in `site/page.html`), then the same key in every other language file. `node scripts/build-site.mjs --check fr de` checks the named translations without building; `node scripts/build-site.mjs` checks all of them and builds. A text keeps the tags and numbers of its English original; the keys used inside attributes (listed in `PLAIN` in the build script) are plain text.

## Publishing a new version

1. Change `CURRENT_VERSION` in `wrangler.jsonc` to the new four-digit version (for example `1300`).
2. Change `RELEASE` and `VERSION` in `scripts/build-site.mjs`, and add the news and changelog entries to `site/page.html`, `site/i18n/en.json` and the translations.
3. Point `LATEST_BUILD` in `src/index.js` at the new release archive; the old download URLs redirect there.
4. Deploy with `npm run deploy`, and push to `master`.

## Looking at the statistics

https://ratiomaster.net/admin shows the last 10,000 version checks by date, country, version and operating system. Every version is split into Windows, macOS, Linux and Other (requests that did not come from the app: browsers, bots, curl), and each of those expands to the concrete systems the User-Agents name (Windows 11, Ubuntu 24.04, macOS 26...). 0.43 and older run on .NET Framework without a compatibility manifest, so they report "Windows 8 or newer" for anything from Windows 8 to Windows 11, and their Mono builds only give a kernel version. Click a day, a version or a system, or use the form, to narrow the window (`?version=0430&os=Windows%207&day=2026-09-06`; `os` takes a family or a concrete name). It asks for a password (HTTP Basic auth; the user name does not matter) and checks it against the `ADMIN_PASSWORD` secret. Change the password with `npx wrangler secret put ADMIN_PASSWORD`. For `npm run dev`, put `ADMIN_PASSWORD=...` in `web/.dev.vars` (ignored by git).

The page reads the newest rows by id in one query, which needs no sort and no index; the grouping and the User-Agent parsing happen in the Worker. Anything else goes through D1 -> ratiomaster-net -> Console in the dashboard, or `npx wrangler d1 execute ratiomaster-net --remote --command "..."`.

```sql
-- `checked_at` is unix seconds (UTC). There is no index, so every query scans the whole table; expect a few seconds.

-- checks per day, last 30 days
SELECT date(checked_at, 'unixepoch') AS day, COUNT(*) AS checks, COUNT(DISTINCT ip) AS installs
FROM version_checks WHERE checked_at > unixepoch('now', '-30 days')
GROUP BY day ORDER BY day DESC;

-- which versions are still out there
SELECT client_version, COUNT(*) FROM version_checks
WHERE checked_at > unixepoch('now', '-30 days') GROUP BY client_version ORDER BY 2 DESC;

-- operating systems (the part of the User-Agent between "(" and ";")
SELECT substr(user_agent, instr(user_agent, '(') + 1, instr(user_agent, ';') - instr(user_agent, '(') - 1) AS os, COUNT(*)
FROM version_checks WHERE checked_at > unixepoch('now', '-30 days') GROUP BY os ORDER BY 2 DESC;
```
