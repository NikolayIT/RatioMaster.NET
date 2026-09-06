// ratiomaster.net Worker.
//
// Three jobs:
//   1. Answer the update check that every RatioMaster.NET install performs
//      (GET /vc.php?v=NNNN, older builds GET /version.html) and log it to D1.
//   2. Redirect the old PHP page URLs to the new single page.
//   3. Serve the password-protected statistics page at /admin (see admin.js).
// Everything else is served from ./public by Cloudflare's static assets.

import { handleAdmin } from "./admin.js";

// User-Agent sent by 0.43 and earlier (VersionChecker.cs):
//   RatioMaster.NET/0430 (Microsoft Windows NT 10.0.26200.0; .NET CLR 4.0.30319.42000; <username>.<cpu count>)
// Builds before 0.42 identify as "NRPG RatioMaster/0410 (...)" and still check in.
// That format carries the Windows user name, which is deliberately parsed out and never stored.
const LEGACY_USER_AGENT = /^(?:RatioMaster\.NET|NRPG RatioMaster)\/(\d{4}) \((.*?); \.NET CLR ([^;]+); (.*)\.(\d+)\)$/;

// User-Agent sent by 1.0 and later (UpdateChecker.cs). It carries no user name:
//   RatioMaster.NET/1000 (Microsoft Windows 10.0.26200; X64; .NET 10.0.11; 32)
const CURRENT_USER_AGENT = /^RatioMaster\.NET\/(\d{4}) \((.*?); ([^;]+); \.NET ([^;]+); (\d+)\)$/;

// The newest build. Every old download URL below ends up here; change it when a new version ships.
const LATEST_BUILD = "https://github.com/NikolayIT/RatioMaster.NET/releases/download/v1.0.0/RatioMaster.NET-win-x64.zip";

const LEGACY_REDIRECTS = {
  "/index.php": "/",
  "/news.php": "/#news",
  "/features.php": "/#features",
  "/history.php": "/#changelog",
  "/screens.php": "/#screenshots",
  "/HISTORY.TXT": "https://raw.githubusercontent.com/NikolayIT/RatioMaster.NET/master/HISTORY.TXT",
  // Archives that used to sit on the old server. They were never linked from the site
  // but other sites link them, so they lead to the newest build instead of 404.
  "/files/RatioMaster.NET.zip": LATEST_BUILD,
  "/files/RatioMaster.NET.rar": LATEST_BUILD,
  "/files/RatioMaster.NET-mono.rar": LATEST_BUILD,
  "/alpha/0.50-alpha1.rar": LATEST_BUILD,
  "/alpha/0.50-alpha2.rar": LATEST_BUILD,
};

export default {
  async fetch(request, env, ctx) {
    const url = new URL(request.url);
    const path = url.pathname;

    if (path === "/vc.php" || path === "/version.html") {
      ctx.waitUntil(logVersionCheck(request, env, url.searchParams.get("v")));
      return new Response(env.CURRENT_VERSION, {
        headers: {
          "content-type": "text/plain; charset=utf-8",
          "cache-control": "no-store",
        },
      });
    }

    if (path === "/admin" || path === "/admin/") {
      return handleAdmin(request, env);
    }

    const target = LEGACY_REDIRECTS[path];
    if (target) {
      return Response.redirect(new URL(target, url).toString(), 301);
    }
    if (path.endsWith(".php")) {
      const page = await env.ASSETS.fetch(new URL("/404.html", url));
      return new Response(page.body, { status: 404, headers: { "content-type": "text/html; charset=utf-8" } });
    }

    return env.ASSETS.fetch(request);
  },
};

async function logVersionCheck(request, env, clientVersion) {
  if (!env.DB) return;
  try {
    const rawUserAgent = request.headers.get("user-agent") || "";
    const legacy = LEGACY_USER_AGENT.exec(rawUserAgent);
    const current = CURRENT_USER_AGENT.exec(rawUserAgent);

    // The legacy format is rewritten without the user name; the current one already has none.
    let userAgent;
    if (legacy) {
      userAgent = `${rawUserAgent.slice(0, rawUserAgent.indexOf("/"))}/${legacy[1]} (${legacy[2]}; .NET CLR ${legacy[3]}; ${legacy[5]})`;
    } else {
      userAgent = rawUserAgent.slice(0, 500);
    }

    const version = clientVersion || (legacy ? legacy[1] : current ? current[1] : null);

    await env.DB.prepare(
      "INSERT INTO version_checks (client_version, country, ip, user_agent) VALUES (?1, ?2, ?3, ?4)"
    )
      .bind(version, request.cf?.country ?? null, request.headers.get("cf-connecting-ip") || null, userAgent)
      .run();
  } catch (err) {
    // Logging must never break the version check itself.
    console.error("version check logging failed:", err);
  }
}
