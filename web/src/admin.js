// /admin: statistics over the most recent version checks, behind a password.
//
// Access is HTTP Basic auth checked against the ADMIN_PASSWORD secret
// (`npx wrangler secret put ADMIN_PASSWORD`; for `wrangler dev` put it in .dev.vars).
// The user name is ignored; only the password counts.
//
// The window is the last WINDOW rows by id. SQLite walks the primary key
// backwards for that and stops after WINDOW rows, so no sort and no index
// are needed and the page costs WINDOW row reads however big the table gets.
// The rows come to the Worker in one query; the grouping, the filters and the
// operating-system split all happen here. ?version=NNNN, ?day=YYYY-MM-DD and
// ?os=<family or concrete system> narrow the window further (version "-"
// means checks that reported no version).

const WINDOW = 10000;

const RECENT = `SELECT id, checked_at, client_version, country, user_agent FROM version_checks ORDER BY id DESC LIMIT ${WINDOW}`;

// The two User-Agent shapes the app sends (see index.js); the legacy one is the
// stored form, with the Windows user name already removed:
//   RatioMaster.NET/1100 (Microsoft Windows 10.0.26200; X64; .NET 10.0.11; 16)
//   RatioMaster.NET/0430 (Microsoft Windows NT 6.2.9200.0; .NET CLR 4.0.30319.42000; 8)
const CURRENT_UA = /^RatioMaster\.NET\/\d{4} \((.*?); [^;]+; \.NET [^;]+; \d+\)$/;
const LEGACY_UA = /^(?:RatioMaster\.NET|NRPG RatioMaster)\/\d{4} \((.*?); \.NET CLR [^;]+; \d+\)$/;

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
  const os = (params.get("os") || "").trim().slice(0, 60);
  return {
    version: version || null,
    day: /^\d{4}-\d{2}-\d{2}$/.test(day) ? day : null,
    os: os || null,
  };
}

async function loadStats(db, filter) {
  const { results: rows } = await db.prepare(RECENT).all();

  const window = { n: rows.length, first_id: null, last_id: null, from_at: null, to_at: null };
  for (const row of rows) {
    row.day = new Date(row.checked_at * 1000).toISOString().slice(0, 10);
    row.os = describeOs(row.user_agent);
    window.first_id = window.first_id === null ? row.id : Math.min(window.first_id, row.id);
    window.last_id = window.last_id === null ? row.id : Math.max(window.last_id, row.id);
    window.from_at = window.from_at === null ? row.checked_at : Math.min(window.from_at, row.checked_at);
    window.to_at = window.to_at === null ? row.checked_at : Math.max(window.to_at, row.checked_at);
  }

  const version = (row) => row.client_version ?? "-";
  const family = (row) => row.os.family;
  const system = (row) => row.os.name;

  const matched = rows.filter(
    (row) =>
      (!filter.version || version(row) === filter.version) &&
      (!filter.day || row.day === filter.day) &&
      (!filter.os || row.os.family === filter.os || row.os.name === filter.os)
  );

  return {
    window,
    allVersions: nest(rows, [version]),
    allSystems: nest(rows, [family, system]),
    matched: matched.length,
    days: nest(matched, [(row) => row.day]).sort((a, b) => (a.key < b.key ? 1 : -1)),
    countries: nest(matched, [(row) => row.country]),
    versions: nest(matched, [version, family, system]),
    systems: nest(matched, [family, system]),
  };
}

// Groups rows by the given key functions, one level per function. Every level
// is sorted by count, then by key; each node is { key, n, children }.
function nest(rows, keys) {
  const root = new Map();
  for (const row of rows) {
    let level = root;
    for (const keyOf of keys) {
      const key = keyOf(row);
      let node = level.get(key);
      if (!node) {
        node = { key, n: 0, children: new Map() };
        level.set(key, node);
      }
      node.n++;
      level = node.children;
    }
  }
  const toArray = (map) =>
    [...map.values()]
      .sort((a, b) => b.n - a.n || String(a.key ?? "").localeCompare(String(b.key ?? "")))
      .map((node) => ({ key: node.key, n: node.n, children: toArray(node.children) }));
  return toArray(root);
}

// Turns a User-Agent into { family, name }: the family is Windows, macOS, Linux
// or Other (requests that did not come from the app), the name is the concrete
// system as far as the User-Agent tells it.
function describeOs(userAgent) {
  const ua = (userAgent || "").trim();
  if (!ua) return { family: "Other", name: "No user agent" };

  const current = CURRENT_UA.exec(ua);
  const legacy = current ? null : LEGACY_UA.exec(ua);
  const match = current || legacy;
  if (!match) {
    if (/^(?:RatioMaster\.NET|NRPG RatioMaster)\b/.test(ua)) return { family: "Other", name: "RatioMaster.NET, unknown system" };
    if (/^mozilla\//i.test(ua)) return { family: "Other", name: "Browser or bot" };
    const product = /^([A-Za-z][\w.+-]{0,29})/.exec(ua);
    return { family: "Other", name: product ? product[1] : "Unrecognized" };
  }

  const os = match[1].trim();
  if (/^Microsoft Windows\b/.test(os)) return { family: "Windows", name: windowsName(os, Boolean(legacy)) };

  const mac = /^macOS (\d+)/.exec(os);
  if (mac) return { family: "macOS", name: `macOS ${mac[1]}` };

  // Mono (0.43 on Unix) and older .NET report the kernel: "Unix 6.12.63.0" is a
  // Linux kernel, "Unix 24.1.0" / "Darwin 24.1.0" a Darwin one, so 20 and up is a Mac.
  const kernel = /^(?:Unix|Darwin) (\d+)/.exec(os);
  if (kernel && Number(kernel[1]) >= 20) {
    const darwin = Number(kernel[1]);
    return { family: "macOS", name: `macOS ${darwin >= 25 ? darwin + 1 : darwin - 9}` };
  }

  return { family: "Linux", name: linuxName(os) };
}

function windowsName(os, legacy) {
  const m = /^Microsoft Windows(?: NT)? (\d+)\.(\d+)(?:\.(\d+))?/.exec(os);
  if (!m) return "Windows, version unknown";
  const release = `${m[1]}.${m[2]}`;
  const build = m[3] ? Number(m[3]) : null;
  switch (release) {
    case "10.0":
      if (build === null) return "Windows 10 or newer";
      if (build >= 22000) return "Windows 11";
      if (build === 20348) return "Windows Server 2022";
      return "Windows 10";
    case "6.3":
      return "Windows 8.1";
    case "6.2":
      // .NET Framework without a compatibility manifest (0.43 and older) reports 6.2
      // on everything from Windows 8 to Windows 11, so the exact version is unknown.
      return legacy ? "Windows 8 or newer" : "Windows 8";
    case "6.1":
      return "Windows 7";
    case "6.0":
      return "Windows Vista";
    case "5.2":
      return "Windows XP x64 or Server 2003";
    case "5.1":
      return "Windows XP";
    case "5.0":
      return "Windows 2000";
    default:
      return `Windows ${release}`;
  }
}

// "Ubuntu 24.04.4 LTS" -> "Ubuntu 24.04", "Debian GNU/Linux 13 (trixie)" -> "Debian 13",
// "Fedora Linux 41 (Workstation Edition)" -> "Fedora 41". A bare kernel version
// ("Unix 6.12.63.0" from Mono, "Linux 6.1.0") names no distribution.
function linuxName(os) {
  if (/^Unix\b/.test(os) || /^Linux(?:\s+[\d.].*)?$/.test(os)) return "Linux, distribution unknown";
  const name = os
    .replace(/\s*\([^)]*\)\s*$/, "")
    .replace(/\bGNU\/Linux\b\s*/, "")
    .replace(/^(\S+) Linux\b\s*/, "$1 ")
    .replace(/\s+LTS$/, "")
    .replace(/(\d+\.\d+)(?:\.\d+)+/, "$1")
    .replace(/\s+/g, " ")
    .trim();
  return name || os;
}

function renderPage(stats, filter) {
  const total = stats.matched;
  const href = (overrides) => escapeHtml(filterHref({ ...filter, ...overrides }));
  const link = (overrides, label) => `<a href="${href(overrides)}">${label}</a>`;

  const days = stats.days.map((day) => ({ cell: link({ day: day.key }, escapeHtml(day.key)), n: day.n }));
  const countries = stats.countries.map((country) => ({ cell: countryLabel(country.key), n: country.n }));
  const versions = stats.versions.map((version) => ({
    cell: link({ version: version.key }, versionLabel(version.key)),
    n: version.n,
    children: version.children.map((family) => ({
      cell: link({ version: version.key, os: family.key }, escapeHtml(family.key)),
      n: family.n,
      children: family.children.map((system) => ({
        cell: link({ version: version.key, os: system.key }, escapeHtml(system.key)),
        n: system.n,
      })),
    })),
  }));
  const systems = stats.systems.map((family) => ({
    cell: link({ os: family.key }, escapeHtml(family.key)),
    n: family.n,
    children: family.children.map((system) => ({ cell: link({ os: system.key }, escapeHtml(system.key)), n: system.n })),
  }));

  const versionOptions = stats.allVersions
    .map(
      (row) =>
        `<option value="${escapeHtml(row.key)}"${row.key === filter.version ? " selected" : ""}>${row.key === "-" ? "Unknown" : escapeHtml(row.key)} (${integer(row.n)})</option>`
    )
    .join("");

  const option = (value, label, n) =>
    `<option value="${escapeHtml(value)}"${value === filter.os ? " selected" : ""}>${escapeHtml(label)} (${integer(n)})</option>`;
  const systemOptions = stats.allSystems
    .map(
      (family) =>
        `<optgroup label="${escapeHtml(family.key)}">${option(family.key, `All ${family.key}`, family.n)}${family.children
          .map((system) => option(system.key, system.key, system.n))
          .join("")}</optgroup>`
    )
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
    .wrap { max-width: none; }
    .stats { display: grid; grid-template-columns: 1fr; gap: 0 40px; }
    @media (min-width: 900px) {
      .stats { grid-template-columns: repeat(2, minmax(0, 1fr)); grid-template-areas: "date country" "version country" "os country"; align-items: start; }
      .stats section { border-bottom: 0; }
      .stats .date { grid-area: date; }
      .stats .country { grid-area: country; }
      .stats .version { grid-area: version; }
      .stats .os { grid-area: os; }
    }
    @media (min-width: 1300px) {
      .stats { grid-template-columns: repeat(4, minmax(0, 1fr)); grid-template-areas: "date country version os"; }
    }
    table { width: 100%; border-collapse: collapse; font-size: 0.93rem; }
    th, td { padding: 4px 8px; text-align: left; border-bottom: 1px solid var(--border); vertical-align: middle; white-space: nowrap; }
    th { color: var(--muted); font-weight: 600; font-size: 0.82rem; text-transform: uppercase; letter-spacing: 0.03em; }
    td:first-child { overflow: hidden; text-overflow: ellipsis; max-width: 0; width: 55%; }
    td:first-child a { color: var(--text); }
    td:first-child a:hover { color: var(--accent); }
    th.num, td.num { text-align: right; font-variant-numeric: tabular-nums; }
    td.bar { width: 25%; min-width: 60px; }
    td.bar i { display: block; height: 10px; min-width: 1px; border-radius: 2px; background: var(--accent); }
    tr.d1 td:first-child { padding-left: 28px; }
    tr.d2 td:first-child { padding-left: 48px; }
    tr.d1 td, tr.d2 td, tr.d1 td:first-child a, tr.d2 td:first-child a { color: var(--muted); }
    tr.d1 td.bar i, tr.d2 td.bar i { opacity: 0.55; }
    .tg { display: inline-block; width: 18px; margin: 0; padding: 0; border: 0; background: none; color: var(--accent); font: inherit; line-height: 1; vertical-align: baseline; }
    button.tg { cursor: pointer; }
    button.tg::before { content: "\\25B8"; }
    tr.open > td > button.tg::before { content: "\\25BE"; }
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
    <p class="hint">Times are UTC. The oldest day in the window is only partially covered. Checks that reached the old server before the DNS change carry no country. The operating system comes from the User-Agent: 0.43 and older say "Windows 8 or newer" for anything from Windows 8 to Windows 11 and cannot name a Linux distribution; "Other" is traffic that did not come from the app. Click a day, a version or a system to filter; the triangles open the split.</p>
    <form class="filter" method="get" action="/admin">
      <label>Version
        <select name="version">
          <option value="">All versions</option>
          ${versionOptions}
        </select>
      </label>
      <label>Operating system
        <select name="os">
          <option value="">All systems</option>
          ${systemOptions}
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
    <section class="date">
      <h2>By date</h2>
      ${table("Day", days, total, "d")}
    </section>
    <section class="country">
      <h2>By country</h2>
      ${table("Country", countries, total, "c")}
    </section>
    <section class="version">
      <h2>By version</h2>
      ${table("Version", versions, total, "v")}
    </section>
    <section class="os">
      <h2>By operating system</h2>
      ${table("System", systems, total, "s")}
    </section>
  </div>
</main>

<footer class="foot">Generated at ${timestamp(Math.floor(Date.now() / 1000))} UTC</footer>

<script>
  document.addEventListener("click", (event) => {
    const button = event.target.closest("button.tg");
    if (button) setOpen(button.closest("tr"), !button.closest("tr").classList.contains("open"));
  });
  function setOpen(row, open) {
    row.classList.toggle("open", open);
    row.querySelector("button.tg").setAttribute("aria-expanded", String(open));
    for (const child of row.closest("table").querySelectorAll('tr[data-parent="' + row.dataset.id + '"]')) {
      child.hidden = !open;
      if (!open && child.classList.contains("open")) setOpen(child, false);
    }
  }
</script>

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
  if (filter.os) parts.push(filter.os === "Other" ? "not from the app" : `on ${escapeHtml(filter.os)}`);
  if (filter.day) parts.push(`on ${escapeHtml(filter.day)}`);
  if (parts.length === 0) {
    return `The last ${integer(w.n)} checks: ${range}, ${integer(stats.days.length)} days.`;
  }
  return `${integer(stats.matched)} of the last ${integer(w.n)} checks (${range}) are ${parts.join(" ")}.`;
}

function filterHref(filter) {
  const params = new URLSearchParams();
  if (filter.version) params.set("version", filter.version);
  if (filter.os) params.set("os", filter.os);
  if (filter.day) params.set("day", filter.day);
  const query = params.toString();
  return query ? `/admin?${query}` : "/admin";
}

// Renders nodes of { cell, n, children? } as a table. Top-level nodes start
// open, deeper ones closed; the triangle buttons toggle them (see the script).
function table(label, nodes, total, prefix) {
  if (nodes.length === 0) return '<p class="muted">Nothing matches.</p>';
  const tree = nodes.some((node) => node.children?.length);
  const rows = [];
  let counter = 0;

  const walk = (list, parent, depth, visible) => {
    for (const node of list) {
      const id = `${prefix}${++counter}`;
      const children = node.children ?? [];
      const open = depth === 0 && children.length > 0;
      const share = total ? (100 * node.n) / total : 0;
      let toggle = "";
      if (tree) {
        toggle = children.length
          ? `<button type="button" class="tg" aria-expanded="${open}" aria-label="Expand"></button>`
          : '<span class="tg"></span>';
      }
      const attributes = `data-id="${id}"${parent ? ` data-parent="${parent}"` : ""} class="d${depth}${open ? " open" : ""}"${visible ? "" : " hidden"}`;
      rows.push(
        `<tr ${attributes}><td>${toggle}${node.cell}</td><td class="num">${integer(node.n)}</td><td class="num">${share.toFixed(1)}%</td><td class="bar"><i style="width:${share.toFixed(2)}%"></i></td></tr>`
      );
      walk(children, id, depth + 1, visible && open);
    }
  };
  walk(nodes, null, 0, true);

  return `<table>
<thead><tr><th>${label}</th><th class="num">Checks</th><th class="num">Share</th><th></th></tr></thead>
<tbody>
${rows.join("\n")}
</tbody>
</table>`;
}

function versionLabel(version) {
  return version === "-" ? '<span class="muted">Unknown</span>' : escapeHtml(version);
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
