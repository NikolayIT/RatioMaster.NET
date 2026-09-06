# ratiomaster.net

The website of RatioMaster.NET, hosted on Cloudflare Workers.

- `public/` is the static site. Cloudflare serves it directly from its static assets storage; those requests never run the Worker.
- `src/index.js` is the Worker. It answers the update check that every installed copy performs (`GET /vc.php?v=NNNN`, older builds `GET /version.html`), logs each check to a D1 database, and 301-redirects the old PHP page URLs to the new page.
- `migrations/` holds the D1 schema, applied with Wrangler.
- `public/files/` and `public/alpha/` keep the download archives that were only ever on the old server.

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
4. `npm run deploy` for the first deployment. Every later push to `master` deploys automatically once the Git integration is connected:
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

## Publishing a new version

1. Change `CURRENT_VERSION` in `wrangler.jsonc` to the new four-digit version (for example `0500`).
2. Update the download link, version number, changelog and news in `public/index.html`.
3. Push to `master`.

## Looking at the statistics

D1 -> ratiomaster-net -> Console in the dashboard, or `npx wrangler d1 execute ratiomaster-net --remote --command "..."`.

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
