import type { APIRoute } from "astro";
import { adminApiPost, type FeedbackReplyResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

/** PRG-Action: Team-Antwort senden (setzt Status Answered + loest Push aus). */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const id = form.get("id")?.toString() ?? "";
  const body = form.get("body")?.toString().trim() ?? "";

  let ok = false;
  if (id.length > 0 && body.length > 0) {
    const response = await adminApiPost<FeedbackReplyResponse>("/api/admin/feedback/reply", {
      TicketId: id,
      Body: body,
    });
    ok = response?.Success === true;
  }

  return new Response(null, {
    status: 303,
    headers: {
      Location: `/intern/feedback/detail/?id=${encodeURIComponent(id)}&action=${ok ? "replied" : "replyfailed"}`,
    },
  });
};
