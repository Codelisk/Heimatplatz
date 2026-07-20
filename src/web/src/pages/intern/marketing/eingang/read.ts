import type { APIRoute } from "astro";
import { adminApiSend } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

/** PRG-Action: Rueckmeldung als gelesen/ungelesen markieren. */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const id = form.get("id")?.toString() ?? "";
  const isRead = form.get("isRead")?.toString() === "true";
  const back = form.get("back")?.toString() === "unread" ? "filter=unread&" : "";

  const ok =
    id.length > 0 &&
    (await adminApiSend("/api/admin/marketing/inbox/read", "POST", { Id: id, IsRead: isRead }));

  return new Response(null, {
    status: 303,
    headers: { Location: `/intern/marketing/eingang/?${back}action=${ok ? "read" : "syncfailed"}` },
  });
};
