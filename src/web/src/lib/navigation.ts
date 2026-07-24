const DEFAULT_LOGIN_REDIRECT = "/favoriten/";
const DEFAULT_REGISTER_REDIRECT = "/";

export function getCurrentReturnTo(url: URL) {
  return `${url.pathname}${url.search}`;
}

export function buildLoginHref(returnTo: string) {
  return `/anmelden/?returnTo=${encodeURIComponent(returnTo)}`;
}

export function buildRegisterHref(returnTo: string) {
  return `/registrieren/?returnTo=${encodeURIComponent(returnTo)}`;
}

function getSafeLocalRedirect(
  value: string | null | undefined,
  baseUrl: URL,
  fallback: string,
  blockedPaths: string[],
) {
  const candidate = value?.trim();
  if (!candidate || !candidate.startsWith("/") || candidate.startsWith("//") || candidate.startsWith("/\\")) {
    return fallback;
  }

  try {
    const resolved = new URL(candidate, baseUrl);
    if (resolved.origin !== baseUrl.origin || blockedPaths.some((path) => resolved.pathname.startsWith(path))) {
      return fallback;
    }
    return `${resolved.pathname}${resolved.search}${resolved.hash}`;
  } catch {
    return fallback;
  }
}

export function getSafeLoginRedirect(value: string | null | undefined, baseUrl: URL) {
  return getSafeLocalRedirect(value, baseUrl, DEFAULT_LOGIN_REDIRECT, ["/anmelden"]);
}

export function getSafeRegisterRedirect(
  value: string | null | undefined,
  baseUrl: URL,
  fallback = DEFAULT_REGISTER_REDIRECT,
) {
  return getSafeLocalRedirect(value, baseUrl, fallback, ["/registrieren", "/anmelden"]);
}
