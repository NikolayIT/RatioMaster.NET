// ratiomaster.net Worker.
//
// Two jobs:
//   1. Answer the update check that every RatioMaster.NET install performs
//      (GET /vc.php?v=NNNN, older builds GET /version.html) and log it to D1.
//   2. Redirect the old PHP page URLs to the new single page.
// Everything else is served from ./public by Cloudflare's static assets.

// User-Agent sent by the app (VersionChecker.cs):
//   RatioMaster.NET/0430 (Microsoft Windows NT 10.0.26200.0; .NET CLR 4.0.30319.42000; <username>.<cpu count>)
// Builds before 0.42 identify as "NRPG RatioMaster/0410 (...)" and still check in.
// The username is deliberately parsed out and never stored.
const APP_USER_AGENT = /^(?:RatioMaster\.NET|NRPG RatioMaster)\/(\d{4}) \((.*?); \.NET CLR ([^;]+); (.*)\.(\d+)\)$/;

const LEGACY_REDIRECTS = {
  "/index.php": "/",
  "/news.php": "/#news",
  "/features.php": "/#features",
  "/history.php": "/#changelog",
  "/screens.php": "/#screenshots",
  "/HISTORY.TXT": "https://raw.githubusercontent.com/NikolayIT/RatioMaster.NET/master/HISTORY.TXT",
  // Same 0.43 build as the GitHub release; the copies on the old server were never linked.
  "/files/RatioMaster.NET.zip": "https://github.com/NikolayIT/RatioMaster.NET/releases/download/0.43/RatioMaster.NET_0.43.zip",
  "/files/RatioMaster.NET.rar": "https://github.com/NikolayIT/RatioMaster.NET/releases/download/0.43/RatioMaster.NET_0.43.zip",
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
    const parsed = APP_USER_AGENT.exec(rawUserAgent);
    // App User-Agent with the username removed; anything else is kept as sent (capped).
    const userAgent = parsed
      ? `${rawUserAgent.slice(0, rawUserAgent.indexOf("/"))}/${parsed[1]} (${parsed[2]}; .NET CLR ${parsed[3]}; ${parsed[5]})`
      : rawUserAgent.slice(0, 500);
    const version = clientVersion || (parsed ? parsed[1] : null);

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
