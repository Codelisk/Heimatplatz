/**
 * Access Token der Web-Session aus dem localStorage lesen (gleicher Key wie
 * PropertyStateScript). Abgelaufene Tokens (Session-ExpiresAt oder JWT-exp)
 * liefern null - dann lieber anonym anfragen, den Refresh erledigt
 * PropertyStateScript beim naechsten apiRequest.
 *
 * Client-only: greift auf window/localStorage zu.
 */
export function readAccessToken(): string | null {
  try {
    const raw = window.localStorage.getItem("heimatplatz:session");
    if (!raw) return null;
    const session = JSON.parse(raw) as { AccessToken?: string; ExpiresAt?: string };
    if (!session.AccessToken) return null;
    if (session.ExpiresAt && new Date(session.ExpiresAt).getTime() <= Date.now()) return null;
    const payload = session.AccessToken.split(".")[1];
    if (payload) {
      const padded = payload.replace(/-/g, "+").replace(/_/g, "/").padEnd(Math.ceil(payload.length / 4) * 4, "=");
      const exp = (JSON.parse(window.atob(padded)) as { exp?: number }).exp;
      if (typeof exp === "number" && exp * 1000 <= Date.now()) return null;
    }
    return session.AccessToken;
  } catch {
    return null;
  }
}
