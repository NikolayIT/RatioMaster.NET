// The languages of the site: the same 38 as the program's interface (UiLanguages.cs), in the same order
// (by native name, Latin script first). `code` is the BCP 47 tag used for lang and hreflang. English lives
// at /, every other language at /<code in lower case>/ (/fr/, /pt-br/, /zh-hans/...).
//
// Shared by the Worker (src/index.js), which sends a visitor to their language, and by the build script
// (scripts/build-site.mjs), which writes one page per language.

export const DEFAULT_LANGUAGE = "en";

export const LANGUAGES = [
  { code: "id", name: "Bahasa Indonesia" },
  { code: "cs", name: "Čeština" },
  { code: "da", name: "Dansk" },
  { code: "de", name: "Deutsch" },
  { code: "et", name: "Eesti" },
  { code: "en", name: "English" },
  { code: "es", name: "Español" },
  { code: "fr", name: "Français" },
  { code: "hr", name: "Hrvatski" },
  { code: "it", name: "Italiano" },
  { code: "lt", name: "Lietuvių" },
  { code: "hu", name: "Magyar" },
  { code: "nl", name: "Nederlands" },
  { code: "nb", name: "Norsk bokmål" },
  { code: "pl", name: "Polski" },
  { code: "pt-BR", name: "Português (Brasil)" },
  { code: "pt", name: "Português (Portugal)" },
  { code: "ro", name: "Română" },
  { code: "sq", name: "Shqip" },
  { code: "sk", name: "Slovenčina" },
  { code: "sl", name: "Slovenščina" },
  { code: "sr-Latn", name: "Srpski (latinica)" },
  { code: "fi", name: "Suomi" },
  { code: "sv", name: "Svenska" },
  { code: "vi", name: "Tiếng Việt" },
  { code: "tr", name: "Türkçe" },
  { code: "el", name: "Ελληνικά" },
  { code: "bg", name: "Български" },
  { code: "ru", name: "Русский" },
  { code: "sr-Cyrl", name: "Српски (ћирилица)" },
  { code: "uk", name: "Українська" },
  { code: "he", name: "עברית", rtl: true },
  { code: "ar", name: "العربية", rtl: true },
  { code: "hi", name: "हिन्दी" },
  { code: "th", name: "ไทย" },
  { code: "ja", name: "日本語" },
  { code: "zh-Hans", name: "简体中文" },
  { code: "zh-Hant", name: "繁體中文" },
];

const BY_CODE = new Map(LANGUAGES.map((language) => [language.code.toLowerCase(), language.code]));

/** The path of a language's page: / for English, /fr/ for French. */
export function pathOf(code) {
  return code === DEFAULT_LANGUAGE ? "/" : `/${code.toLowerCase()}/`;
}

/**
 * The site language for one language tag, as UiLanguages.Match picks it in the program: the tag itself or its
 * nearest parent (fr-CA is fr, pt-PT is pt), then what a parent chain cannot settle: Norwegian, the old codes
 * for Hebrew and Indonesian, and Chinese and Serbian by script or region. Null when nothing fits.
 */
export function matchLanguage(tag) {
  const parts = String(tag ?? "").trim().toLowerCase().split(/[-_]/).filter(Boolean);
  for (let length = parts.length; length > 0; length--) {
    const code = BY_CODE.get(parts.slice(0, length).join("-"));
    if (code) return code;
  }

  const has = (part) => parts.includes(part);
  switch (parts[0]) {
    case "no":
    case "nn":
      return "nb";
    case "iw":
      return "he";
    case "in":
      return "id";
    case "zh":
      return has("hant") || has("tw") || has("hk") || has("mo") ? "zh-Hant" : "zh-Hans";
    case "sr":
      // Serbia writes Cyrillic by default; Montenegro and an explicit Latin script use the Latin alphabet.
      return has("latn") || has("me") ? "sr-Latn" : "sr-Cyrl";
    default:
      return null;
  }
}

/** The best site language for an Accept-Language header, by quality and then order; null when none fits. */
export function pickLanguage(acceptLanguage) {
  const ranges = String(acceptLanguage ?? "")
    .split(",")
    .map((part, index) => {
      const [tag, ...parameters] = part.trim().split(";");
      const q = parameters.map((p) => p.trim()).find((p) => p.startsWith("q="));
      return { tag: tag.trim(), quality: q === undefined ? 1 : Number(q.slice(2)), index };
    })
    .filter((range) => range.tag && range.tag !== "*" && range.quality > 0)
    .sort((a, b) => b.quality - a.quality || a.index - b.index);

  for (const range of ranges) {
    const code = matchLanguage(range.tag);
    if (code) return code;
  }
  return null;
}
