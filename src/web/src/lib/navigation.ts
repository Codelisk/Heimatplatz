const DEFAULT_LOGIN_REDIRECT = "/favoriten/";

export function getCurrentReturnTo(url: URL) {
  return `${url.pathname}${url.search}`;
}

export function buildLoginHref(returnTo: string) {
  return `/anmelden/?returnTo=${encodeURIComponent(returnTo)}`;
}

export function getSafeLoginRedirect(value: string | null | undefined, baseUrl: URL) {
  const candidate = value?.trim();
  if (!candidate || !candidate.startsWith("/") || candidate.startsWith("//") || candidate.startsWith("/\\")) {
    return DEFAULT_LOGIN_REDIRECT;
  }

  try {
    const resolved = new URL(candidate, baseUrl);
    if (resolved.origin !== baseUrl.origin || resolved.pathname.startsWith("/anmelden")) {
      return DEFAULT_LOGIN_REDIRECT;
    }
    return `${resolved.pathname}${resolved.search}${resolved.hash}`;
  } catch {
    return DEFAULT_LOGIN_REDIRECT;
  }
}
