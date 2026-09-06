// /admin: statistics over the most recent version checks, behind a password.
//
// Access is HTTP Basic auth checked against the ADMIN_PASSWORD secret
// (`npx wrangler secret put ADMIN_PASSWORD`; for `wrangler dev` put it in .dev.vars).
// The user name is ignored; only the password counts.
//
// The window is the last WINDOW rows by id. SQLite walks the primary key
// backwards for that and stops after WINDOW rows, so no sort and no index
// are needed and the page costs a few thousand row reads however big the
// table gets. ?version=NNNN and ?day=YYYY-MM-DD narrow the window further
// (version "-" means checks that reported no version).

const WINDOW = 10000;

const RECENT = `SELECT id, checked_at, client_version, country FROM version_checks ORDER BY id DESC LIMIT ${WINDOW}`;

export async function handleAdmin(request, env) {
  if (!env.ADMIN_PASSWORD) {
    return text("The admin page is not configured: set the ADMIN_PASSWORD secret.", 503);
  }
  if (!(await isAuthorized(request, env.ADMIN_PASSWORD))) {
    return text("Authentication required.", 401, {
      "www-authenticate": 'Basic realm="RatioMaster.NET admin", charset="UTF-8"',
    });
  }
  if (!env.DB) {
    return text("No database is bound to this Worker.", 503);
  }

  const filter = parseFilter(new URL(request.url).searchParams);
  const stats = await loadStats(env.DB, filter);
  return new Response(renderPage(stats, filter), { headers: headers("text/html; charset=utf-8") });
}

async function isAuthorized(request, password) {
  const header = request.headers.get("authorization") || "";
  if (!header.startsWith("Basic ")) return false;

  let credentials;
  try {
    credentials = new TextDecoder().decode(Uint8Array.from(atob(header.slice(6).trim()), (c) => c.charCodeAt(0)));
  } catch {
    return false;
  }
  const colon = credentials.indexOf(":");
  const supplied = colon < 0 ? credentials : credentials.slice(colon + 1);

  const encoder = new TextEncoder();
  const a = encoder.encode(supplied);
  const b = encoder.encode(password);
  return a.byteLength === b.byteLength && crypto.subtle.timingSafeEqual(a, b);
}

function parseFilter(params) {
  const version = (params.get("version") || "").trim().slice(0, 20);
  const day = (params.get("day") || "").trim();
  return {
    version: version || null,
    day: /^\d{4}-\d{2}-\d{2}$/.test(day) ? day : null,
  };
}

async function loadStats(db, filter) {
  const conditions = [];
  const binds = [];
  if (filter.version === "-") {
    conditions.push("client_version IS NULL");
  } else if (filter.version) {
    binds.push(filter.version);
    conditions.push(`client_version = ?${binds.length}`);
  }
  if (filter.day) {
    binds.push(filter.day);
    conditions.push(`date(checked_at, 'unixepoch') = ?${binds.length}`);
  }
  const filtered = `SELECT * FROM (${RECENT})${conditions.length ? ` WHERE ${conditions.join(" AND ")}` : ""}`;
  const query = (sql) => db.prepare(sql).bind(...binds);

  const [window, allVersions, byDay, byCountry, byVersion] = await db.batch([
    db.prepare(`SELECT COUNT(*) AS n, MIN(id) AS first_id, MAX(id) AS last_id, MIN(checked_at) AS from_at, MAX(checked_at) AS to_at FROM (${RECENT})`),
    db.prepare(`SELECT client_version AS version, COUNT(*) AS n FROM (${RECENT}) GROUP BY version ORDER BY n DESC, version`),
    query(`SELECT date(checked_at, 'unixepoch') AS day, COUNT(*) AS n FROM (${filtered}) GROUP BY day ORDER BY day DESC`),
    query(`SELECT country, COUNT(*) AS n FROM (${filtered}) GROUP BY country ORDER BY n DESC, country`),
    query(`SELECT client_version AS version, COUNT(*) AS n FROM (${filtered}) GROUP BY version ORDER BY n DESC, version`),
  ]);

  return {
    window: window.results[0],
    allVersions: allVersions.results,
    matched: byDay.results.reduce((sum, row) => sum + row.n, 0),
    days: byDay.results,
    countries: byCountry.results,
    versions: byVersion.results,
  };
}

function renderPage(stats, filter) {
  const total = stats.matched;
  const href = (overrides) => escapeHtml(filterHref({ ...filter, ...overrides }));

  const days = stats.days.map((row) => [`<a href="${href({ day: row.day })}">${escapeHtml(row.day)}</a>`, row.n]);
  const countries = stats.countries.map((row) => [countryLabel(row.country), row.n]);
  const versions = stats.versions.map((row) => [
    `<a href="${href({ version: row.version ?? "-" })}">${versionLabel(row.version)}</a>`,
    row.n,
  ]);

  const options = stats.allVersions
    .map((row) => {
      const value = row.version ?? "-";
      const selected = value === filter.version ? " selected" : "";
      return `<option value="${escapeHtml(value)}"${selected}>${row.version ? escapeHtml(row.version) : "Unknown"} (${integer(row.n)})</option>`;
    })
    .join("");

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="robots" content="noindex, nofollow">
  <title>Version checks - RatioMaster.NET admin</title>
  <link rel="icon" href="/favicon.ico">
  <link rel="stylesheet" href="/style.css">
  <style>
    .filter { display: flex; flex-wrap: wrap; align-items: flex-end; gap: 12px 16px; margin-top: 18px; }
    .filter label { display: flex; flex-direction: column; gap: 4px; color: var(--muted); font-size: 0.85rem; }
    .filter select, .filter input { min-width: 170px; padding: 6px 8px; font: inherit; color: var(--text); background: var(--panel); border: 1px solid var(--border); border-radius: 4px; color-scheme: dark; }
    .filter .btn { margin: 0; padding: 6px 16px; font: inherit; font-weight: 600; cursor: pointer; }
    .stats { display: grid; grid-template-columns: 1fr; gap: 0 40px; }
    @media (min-width: 900px) { .stats { grid-template-columns: repeat(3, minmax(0, 1fr)); } .stats section { border-bottom: 0; } }
    table { width: 100%; border-collapse: collapse; font-size: 0.93rem; }
    th, td { padding: 4px 8px; text-align: left; border-bottom: 1px solid var(--border); vertical-align: middle; white-space: nowrap; }
    th { color: var(--muted); font-weight: 600; font-size: 0.82rem; text-transform: uppercase; letter-spacing: 0.03em; }
    td:first-child { overflow: hidden; text-overflow: ellipsis; max-width: 0; width: 55%; }
    td:first-child a { color: var(--text); }
    td:first-child a:hover { color: var(--accent); }
    th.num, td.num { text-align: right; font-variant-numeric: tabular-nums; }
    td.bar { width: 25%; min-width: 60px; }
    td.bar i { display: block; height: 10px; min-width: 1px; border-radius: 2px; background: var(--accent); }
    .flag { width: 20px; height: auto; vertical-align: -2px; margin-right: 7px; border-radius: 2px; box-shadow: 0 0 0 1px rgba(0, 0, 0, 0.3); }
    .code, .muted { color: var(--muted); }
    .code { font-size: 0.85em; margin-left: 5px; }
  </style>
</head>
<body>

<header class="top">
  <nav class="wrap nav">
    <a class="brand" href="/" aria-label="RatioMaster.NET home">
      <svg class="mark" viewBox="0 0 32 32" width="34" height="34" aria-hidden="true" focusable="false">
        <rect x="5" y="5" width="22" height="22" rx="4" transform="rotate(45 16 16)" fill="#ff9d00"/>
        <path d="M16 7.5 L23 15.5 H19.5 V24.5 H12.5 V15.5 H9 Z" fill="#2a2b1f"/>
      </svg>
      <span class="word">RatioMaster<em>.NET</em></span>
    </a>
    <ul>
      <li><a href="/">Home</a></li>
      <li><a href="/admin">Version checks</a></li>
    </ul>
  </nav>
</header>

<main class="wrap">
  <section>
    <h1>Version checks</h1>
    <p class="lead">${summary(stats, filter)}</p>
    <p class="hint">Times are UTC. The oldest day in the window is only partially covered. Checks that reached the old server before the DNS change carry no country. Click a day or a version to filter.</p>
    <form class="filter" method="get" action="/admin">
      <label>Version
        <select name="version">
          <option value="">All versions</option>
          ${options}
        </select>
      </label>
      <label>Day
        <input type="date" name="day" value="${escapeHtml(filter.day ?? "")}">
      </label>
      <button type="submit" class="btn primary">Apply</button>
      <a class="btn" href="/admin">Clear</a>
    </form>
  </section>

  <div class="stats">
    <section>
      <h2>By date</h2>
      ${table("Day", days, total)}
    </section>
    <section>
      <h2>By country</h2>
      ${table("Country", countries, total)}
    </section>
    <section>
      <h2>By version</h2>
      ${table("Version", versions, total)}
    </section>
  </div>
</main>

<footer class="foot">Generated at ${timestamp(Math.floor(Date.now() / 1000))} UTC</footer>

</body>
</html>
`;
}

function summary(stats, filter) {
  const w = stats.window;
  if (!w.n) return "No version checks have been logged yet.";

  const range = `ids ${integer(w.first_id)} to ${integer(w.last_id)}, from ${timestamp(w.from_at)} to ${timestamp(w.to_at)} UTC`;
  const parts = [];
  if (filter.version) parts.push(filter.version === "-" ? "with no version" : `of version ${escapeHtml(filter.version)}`);
  if (filter.day) parts.push(`on ${escapeHtml(filter.day)}`);
  if (parts.length === 0) {
    return `The last ${integer(w.n)} checks: ${range}, ${integer(stats.days.length)} days.`;
  }
  return `${integer(stats.matched)} of the last ${integer(w.n)} checks (${range}) are ${parts.join(" ")}.`;
}

function filterHref(filter) {
  const params = new URLSearchParams();
  if (filter.version) params.set("version", filter.version);
  if (filter.day) params.set("day", filter.day);
  const query = params.toString();
  return query ? `/admin?${query}` : "/admin";
}

function table(label, rows, total) {
  if (rows.length === 0) return '<p class="muted">Nothing matches.</p>';
  const body = rows
    .map(([cell, n]) => {
      const share = total ? (100 * n) / total : 0;
      return `<tr><td>${cell}</td><td class="num">${integer(n)}</td><td class="num">${share.toFixed(1)}%</td><td class="bar"><i style="width:${share.toFixed(2)}%"></i></td></tr>`;
    })
    .join("\n");
  return `<table>
<thead><tr><th>${label}</th><th class="num">Checks</th><th class="num">Share</th><th></th></tr></thead>
<tbody>
${body}
</tbody>
</table>`;
}

function versionLabel(version) {
  return version ? escapeHtml(version) : '<span class="muted">Unknown</span>';
}

function countryLabel(code) {
  if (!code || code === "XX") return '<span class="muted">Unknown</span>';
  if (code === "T1") return 'Tor network<span class="code">T1</span>';
  if (!/^[A-Z]{2}$/.test(code)) return `<span class="muted">${escapeHtml(code)}</span>`;

  const lower = code.toLowerCase();
  const emoji = String.fromCodePoint(...[...code].map((c) => 0x1f1e6 + c.charCodeAt(0) - 65));
  const flag = `<img class="flag" src="https://flagcdn.com/w20/${lower}.png" srcset="https://flagcdn.com/w40/${lower}.png 2x" width="20" alt="${emoji}" loading="lazy">`;
  return `${flag}${escapeHtml(countryName(code))}<span class="code">${code}</span>`;
}

function countryName(code) {
  try {
    return new Intl.DisplayNames(["en"], { type: "region" }).of(code) || code;
  } catch {
    return code;
  }
}

function integer(n) {
  return Number(n).toLocaleString("en-US");
}

function timestamp(unixSeconds) {
  return new Date(unixSeconds * 1000).toISOString().slice(0, 19).replace("T", " ");
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function headers(contentType, extra = {}) {
  return {
    "content-type": contentType,
    "cache-control": "no-store",
    "x-robots-tag": "noindex, nofollow",
    "x-content-type-options": "nosniff",
    "referrer-policy": "no-referrer",
    ...extra,
  };
}

function text(message, status, extra = {}) {
  return new Response(message, { status, headers: headers("text/plain; charset=utf-8", extra) });
}
