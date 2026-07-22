import type { APIRoute } from "astro";
import { adminApiPost, type FeedbackStatusResponse } from "@/lib/server/admin-api";
import { rejectCrossSite } from "@/lib/server/csrf";

const ALLOWED_STATUSES = new Set(["Open", "InProgress", "Answered", "Closed"]);

/** PRG-Action: Status einer Anfrage manuell setzen. */
export const POST: APIRoute = async ({ request }) => {
  const blocked = rejectCrossSite(request);
  if (blocked) return blocked;

  const form = await request.formData();
  const id = form.get("id")?.toString() ?? "";
  const status = form.get("status")?.toString() ?? "";

  let ok = false;
  if (id.length > 0 && ALLOWED_STATUSES.has(status)) {
    const response = await adminApiPost<FeedbackStatusResponse>("/api/admin/feedback/status", {
      TicketId: id,
      Status: status,
    });
    ok = response?.Success === true;
  }

  return new Response(null, {
    status: 303,
    headers: {
      Location: `/intern/feedback/detail/?id=${encodeURIComponent(id)}&action=${ok ? "status" : "statusfailed"}`,
    },
  });
};
