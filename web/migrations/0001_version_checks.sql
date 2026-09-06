-- One row per update check performed by a RatioMaster.NET install.
CREATE TABLE IF NOT EXISTS version_checks (
  id             INTEGER PRIMARY KEY AUTOINCREMENT,
  checked_at     INTEGER NOT NULL DEFAULT (CAST(strftime('%s', 'now') AS INTEGER)), -- unix seconds, UTC
  client_version TEXT,    -- e.g. 0430, from ?v= or, when that is missing, from the User-Agent
  country        TEXT,    -- ISO 3166-1 alpha-2, from Cloudflare
  ip             TEXT,    -- client address as seen by Cloudflare, IPv4 or IPv6 text form
  user_agent     TEXT     -- as sent, minus the Windows username the app includes
);

-- No index on purpose: it cost 10% of the storage and the Worker only inserts.
-- Stats queries scan the table (a few seconds over the full history), which is fine for occasional use.
