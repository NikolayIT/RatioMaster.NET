// Builds the site into dist/site, the folder Cloudflare serves: public/ as it is, plus one page per language
// made from site/page.html and site/i18n/<code>.json. English goes to /, every other language to /<code>/
// (lower case). Wrangler runs this before every deploy and dev session (the "build" entry in wrangler.jsonc).
//
//   node scripts/build-site.mjs              check every translation and build
//   node scripts/build-site.mjs --check fr   only check the named translations (for translators)
//
// The page uses two kinds of placeholders. {{key}} is a text from the language's JSON; {{@name}} is filled in
// here (lang, dir, url, home, alternates, langName, langMenu, version, release, and "date YYYY-MM-DD").

import { existsSync, mkdirSync, readdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";
import { DEFAULT_LANGUAGE, LANGUAGES, pathOf } from "../src/languages.js";

const SITE = "https://ratiomaster.net";

// The release the download buttons point at, and the version shown next to the name.
const RELEASE = "v1.3.1";
const VERSION = "1.3.1";

// Texts that also appear inside attributes. They are plain text (no tags) and are escaped wherever they go;
// every other text is an HTML fragment that may carry the same tags as the English one.
const PLAIN = new Set(["download.lead", "meta.ogDescription", "nav.homeLabel", "nav.language", "screenshots.mainAlt", "screenshots.addAlt"]);

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const template = readFileSync(join(root, "site", "page.html"), "utf8");
const english = readTexts(DEFAULT_LANGUAGE);

const args = process.argv.slice(2);
const checkOnly = args[0] === "--check";
const codes = checkOnly && args.length > 1 ? args.slice(1) : LANGUAGES.map((language) => language.code);

const problems = [];
for (const key of template.matchAll(/\{\{(?!@)([^}]+)\}\}/g)) {
  if (!(key[1] in english)) problems.push(`page.html: {{${key[1]}}} has no English text`);
}
for (const code of codes) {
  if (!LANGUAGES.some((language) => language.code === code)) problems.push(`${code}: not a site language`);
  else if (code !== DEFAULT_LANGUAGE) problems.push(...check(code, readTexts(code)));
}
if (problems.length > 0) {
  console.error(problems.join("\n"));
  console.error(`${problems.length} problem(s) in the translations; nothing was built.`);
  process.exit(1);
}
if (checkOnly) {
  console.log(`OK: ${codes.join(", ")}`);
  process.exit(0);
}

// Files are overwritten in place and only what the build no longer produces is removed: a running `wrangler dev`
// keeps dist/site open, and on Windows the folder itself cannot be deleted then.
const out = join(root, "dist", "site");
const built = new Set();
const write = (file, content) => {
  mkdirSync(dirname(file), { recursive: true });
  writeFileSync(file, content);
  built.add(file);
};
for (const file of filesIn(join(root, "public"))) {
  write(join(out, relative(join(root, "public"), file)), readFileSync(file));
}
for (const language of LANGUAGES) {
  const page = render(language, language.code === DEFAULT_LANGUAGE ? english : readTexts(language.code));
  write(join(out, pathOf(language.code), "index.html"), page);
}
for (const file of filesIn(out)) {
  if (!built.has(file)) rmSync(file);
}
console.log(`Built ${LANGUAGES.length} pages into dist/site.`);

function filesIn(folder) {
  if (!existsSync(folder)) return [];
  return readdirSync(folder, { recursive: true, withFileTypes: true })
    .filter((entry) => entry.isFile())
    .map((entry) => join(entry.parentPath, entry.name));
}

function readTexts(code) {
  const file = join(root, "site", "i18n", `${code}.json`);
  if (!existsSync(file)) return {};
  return JSON.parse(readFileSync(file, "utf8"));
}

/** Everything that would make a translation unusable: missing or extra texts, changed tags, lost numbers. */
function check(code, texts) {
  const found = [];
  for (const key of Object.keys(english)) {
    const source = english[key];
    const text = texts[key];
    if (typeof text !== "string" || text.trim() === "") {
      found.push(`${code}: "${key}" is missing`);
      continue;
    }
    if (PLAIN.has(key) && /[<>]/.test(text)) found.push(`${code}: "${key}" must be plain text (it is used in an attribute)`);
    const want = tags(source).join(" ");
    const got = tags(text).join(" ");
    if (want !== got) found.push(`${code}: "${key}" has tags ${got || "(none)"} instead of ${want || "(none)"}`);
    for (const number of new Set(source.match(/\d+(?:\.\d+)*/g) ?? [])) {
      if (!text.includes(number)) found.push(`${code}: "${key}" lost the number ${number}`);
    }
    if (source.startsWith("RM: ") && !/^RM\s?[:：]/.test(text)) found.push(`${code}: "${key}" must start with "RM:"`);
    if (text.includes("{{")) found.push(`${code}: "${key}" contains "{{"`);
  }
  for (const key of Object.keys(texts)) {
    if (!(key in english)) found.push(`${code}: "${key}" is not an English key`);
  }
  return found;
}

/** The tags of an HTML fragment with their attributes, sorted, so a translation may reorder them. */
function tags(html) {
  return (html.match(/<\/?[a-z][^>]*>/gi) ?? []).map((tag) => tag.replace(/\s+/g, " ")).sort();
}

function render(language, texts) {
  const { code } = language;
  const escape = (value) => value.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
  const dateFormat = new Intl.DateTimeFormat(code === DEFAULT_LANGUAGE ? "en-GB" : code, {
    year: "numeric",
    month: "long",
    day: "numeric",
    timeZone: "UTC",
  });

  // In right-to-left text a dot that starts a Latin word (".NET" after Arabic or Hebrew) takes the direction of
  // the sentence and shows up after the word ("NET."); a left-to-right mark in front keeps it where it belongs.
  const ltrDot = language.rtl ? (text) => text.replace(/(^|[^A-Za-z0-9])\.NET/g, "$1‎.NET") : (text) => text;

  const special = {
    lang: code,
    dir: language.rtl ? "rtl" : "ltr",
    url: SITE + pathOf(code),
    home: pathOf(code),
    langName: language.name,
    version: VERSION,
    release: RELEASE,
    alternates: [
      ...LANGUAGES.map((other) => `  <link rel="alternate" hreflang="${other.code}" href="${SITE}${pathOf(other.code)}">`),
      `  <link rel="alternate" hreflang="x-default" href="${SITE}/">`,
    ].join("\n"),
    langMenu: LANGUAGES.map((other) => {
      const current = other.code === code ? ' aria-current="page"' : "";
      const dir = other.rtl ? "rtl" : "ltr";
      return `            <li><a href="${pathOf(other.code)}" hreflang="${other.code}" lang="${other.code}" dir="${dir}" data-lang="${other.code}"${current}>${other.name}</a></li>`;
    }).join("\n"),
  };

  const page = template.replace(/\{\{(@?)([^}]+)\}\}/g, (placeholder, isSpecial, name) => {
    if (isSpecial) {
      const date = /^date (\d{4}-\d{2}-\d{2})$/.exec(name);
      if (date) return dateFormat.format(new Date(`${date[1]}T00:00:00Z`));
      if (name in special) return special[name];
      throw new Error(`page.html: unknown placeholder ${placeholder}`);
    }
    const text = texts[name];
    if (typeof text !== "string") throw new Error(`${code}: no text for ${placeholder}`);
    return PLAIN.has(name) ? escape(ltrDot(text)) : ltrDot(text);
  });

  // A full-width colon (Chinese, Japanese) carries its own space, so the one the page puts before a link goes.
  return page.replace(/：[ \t]+(?=<a )/g, "：");
}
