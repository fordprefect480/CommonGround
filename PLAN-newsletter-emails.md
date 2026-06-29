# Newsletter vs other membership emails (server-side Resend templates)

> Implementation plan. Authored against the `claude/vigilant-noether-ur8x3e` branch.
> Build/test/EF tooling (`dotnet`) was unavailable in the authoring environment —
> implement and verify this locally.

## Concept

The real split is **domain-based**: **newsletters** members can opt in/out of vs
**all other membership/account emails**. Today the distinction is implicit in which
code path runs, and the Resend template is fetched and stitched **client-side** then
sent as raw `HtmlBody`.

Model it as a single boolean **`IsNewsletter`** on `SentEmail` — the flag *is* the
rule (`IsNewsletter ⇒ unsubscribe required`). Chosen in the editor via a **toggle
switch**, defaulting **on** (newsletter).

## Key SDK constraint (drives everything)

In Resend 0.5.1, `EmailMessage.Template` is
`EmailMessageTemplate { Guid TemplateId; Dictionary<string,object>? Variables }`, and
**using it is mutually exclusive with `HtmlBody`/`TextBody`**. Consequences:

- Email **content moves into template variables**; Resend renders both HTML and the
  plain-text part. We stop building `TextBody` (and stop the AngleSharp
  `HtmlToTextAsync` step) for templated sends.
- We can no longer post-process the rendered HTML, so the **per-recipient unsubscribe
  URL becomes a template variable**, not a string-replace.
- `List-Unsubscribe` / `List-Unsubscribe-Post` are message **headers**, independent of
  the template — they stay.

## Send-site classification (from the audit)

| Send site | Audience | Template? |
|---|---|---|
| Newsletter composer (`SendNewsletterEndpoint`) | members | **Newsletter template** |
| Membership welcome (`MembershipActivationService` → `MembershipWelcomeEmail`) | member | **Membership template** |
| Password reset (`IdentityEmailSender` → `PasswordResetEmail`) | member | **Membership template** |
| Leased-bed **assignment** (`LeasedBedNotifications.SendAssignmentAsync`) | member | **Membership template** |
| Leased-bed **application-received** (`SendApplicationReceivedAsync` → `AdminNotificationEmail`) | admin/internal | **Raw (exception)** |
| Contact form (`SendContactMessageEndpoint` → `RecipientAddress`) | admin/internal | **Raw (exception)** |
| Test email (`SendTestEmailEndpoint`) | n/a, not recorded | out of scope |

Rule of thumb: **member-facing → templated; sent to the admin/internal notification
address → raw.**

## Confirmed decisions

- **A. Unsubscribe → dedicated template variable `RESEND_UNSUBSCRIBE_URL`** (matches the
  existing `{{{RESEND_UNSUBSCRIBE_URL}}}` placeholder constant). The newsletter
  template's footer chrome references `{{{RESEND_UNSUBSCRIBE_URL}}}`; we pass
  `Variables["RESEND_UNSUBSCRIBE_URL"] = perRecipientUrl`.
- **B. One shared membership template.** All non-newsletter member emails use the single
  2nd template, passing their inner content as a `BODY` variable; the system builders
  are refactored to emit **HTML fragments** instead of full `<!doctype html>` documents.
- **C. Keep the `List-Unsubscribe` headers** on newsletter sends.
- **Toggle default `true`** for both `EmailCompose` and `MemberEmailModal`.

> If the published templates use different variable names than `BODY` /
> `RESEND_UNSUBSCRIBE_URL`, those names are the only thing that changes below.

---

## Backend

1. **Second template config** — `Email/EmailOptions.cs`: keep `TemplateId` (newsletter);
   add `public string? TransactionalTemplateId { get; set; }` (membership). Resolver:
   ```csharp
   public Guid? TemplateIdFor(bool isNewsletter)
   {
       var raw = isNewsletter ? TemplateId : (TransactionalTemplateId ?? TemplateId);
       return Guid.TryParse(raw, out var id) ? id : null;
   }
   ```
   (Returns `Guid?` because the SDK wants a `Guid`.)

2. **Wire config through AppHost** + appsettings — add the
   `resend-transactional-template-id` parameter → `Email__TransactionalTemplateId` block
   in `CommonGround.AppHost/AppHost.cs` (next to the existing `resend-template-id` block
   at ~123-127), and `"TransactionalTemplateId": ""` under `Email` in
   `CommonGround.Server/appsettings.json`.

3. **Model + EF + migration**
   - `Data/SentEmail.cs`: add `public bool IsNewsletter { get; set; }`.
   - `Data/AppDbContext.cs` (the `Entity<SentEmail>` block, ~line 185):
     `b.Property(e => e.IsNewsletter).HasDefaultValue(false);`
   - Migration: `dotnet ef migrations add AddSentEmailIsNewsletter --project CommonGround.Server`.
     Expected `Up`: `AddColumn<bool>("IsNewsletter", "SentEmails", nullable: false, defaultValue: false)`.
     Existing rows + system mail backfill to non-newsletter. Let the tool regenerate
     `AppDbContextModelSnapshot.cs`. (Migrations live in
     `CommonGround.Server/Data/Migrations/`; applied at startup via
     `Database.MigrateAsync()` in `Program.cs`.)
   - **History content:** templated sends have no final HTML on our side, so store the
     **content fragment we passed** (the `BODY` variable value) in `SentEmail.HtmlBody`,
     and leave `TextBody` empty for templated sends.

4. **`TransactionalEmailSender` gains a templated path** — `Email/TransactionalEmailSender.cs`:
   - Keep the existing **raw** `SendAsync(subject, htmlBody, textBody, recipient, replyTo, ct)`
     for the two internal/admin exceptions; record with `IsNewsletter = false`.
   - Add **`SendTemplatedAsync(subject, Guid templateId, IReadOnlyDictionary<string,object> variables,
     string bodyForHistory, Recipient, replyTo, ct)`** that sets
     `message.Template = new EmailMessageTemplate { TemplateId = templateId, Variables = ... }`,
     leaves `HtmlBody`/`TextBody` null, records `IsNewsletter = false` and stores
     `bodyForHistory` in `SentEmail.HtmlBody`.

5. **Refactor member-facing system builders to fragments** (feed `Variables["BODY"]`):
   - `Members/MembershipWelcomeEmail.cs` — drop the `<!doctype html><body …>` wrapper;
     emit the inner `<h2>…</h2><p>…</p>` content only. Drop `BuildText` (Resend renders
     text). Caller (`Members/MembershipActivationService.cs:98`) switches to
     `SendTemplatedAsync(Subject, membershipTemplateId, {["BODY"]=html}, html, recipient)`.
   - `Auth/PasswordResetEmail.cs` (`Auth/IdentityEmailSender.cs:52`) — same fragment
     treatment, same `SendTemplatedAsync` call.
   - `LeasedBeds/LeasedBedEmails.cs` `BuildAssignmentHtml` (used by
     `LeasedBedNotifications.SendAssignmentAsync`) — fragment + `SendTemplatedAsync`. The
     **application-received** path (`SendApplicationReceivedAsync`) stays on raw `SendAsync`
     (admin notification). Note `Wrap()` is shared by the applied/waitlisted (admin) bodies
     too — keep `Wrap` for those, only the assignment body becomes a fragment.
   - Each call resolves the id via `emailOptions.Value.TemplateIdFor(isNewsletter:false)`;
     if it returns null (template unconfigured) fall back to the existing raw `SendAsync`
     so mail still goes out.

6. **Newsletter composer** — `Email/SendNewsletterEndpoint.cs`:
   - Add `bool? IsNewsletter` to `Request`, default **true**; store on `SentEmail`.
   - Resolve `templateId = options.Value.TemplateIdFor(req.IsNewsletter ?? true)`.
   - Per recipient, build `Variables`: `["BODY"] = body`, and **if newsletter** also
     `["RESEND_UNSUBSCRIBE_URL"] = BuildUnsubscribeUrl(r)`. Set `message.Template`, leave
     `HtmlBody`/`TextBody` null.
   - **If newsletter**, set the `List-Unsubscribe` / `List-Unsubscribe-Post` headers via
     the extracted helper; skip them for non-newsletter.
   - Drop the `{{{RESEND_UNSUBSCRIBE_URL}}}` sentinel replacement and the AngleSharp text
     generation for the templated path. Recipient resolution (three modes,
     `IsSubscribedToMailingList` filtering) is unchanged.
   - Fallback: if `templateId` is null, keep today's raw-HTML send so the composer still
     works unconfigured.

7. **Extract unsubscribe helper** — `Email/UnsubscribeInjector.cs` exposes building the
   per-recipient URL + the two headers (the body-sentinel replacement is retired under
   templating). Keeps using `UnsubscribeTokenService`.

8. **`GetEmailTemplateEndpoint`** — still used for the **editor preview only**. Add the
   `isNewsletter` query param and resolve via `TemplateIdFor`, so the preview chrome
   matches the selected template. (Send no longer goes through this.)

9. **DTOs** — add `bool IsNewsletter` to `Email/ListSentEmailsEndpoint.cs` `Item`,
   `Email/GetSentEmailEndpoint.cs` `Result`, `Members/ListMemberEmailsEndpoint.cs` `Item`
   (from `r.SentEmail.IsNewsletter`).

## Frontend

10. **API** — `frontend/src/api/email.ts`: add `isNewsletter: boolean` to
    `SentEmailListItem`, `SentEmailDetail`, `MemberEmailListItem`; `fetchEmailTemplate(isNewsletter)`
    passes `?isNewsletter=`; `sendNewsletter(...)` sends `isNewsletter` in the payload.
    (Send still posts the composed **body fragment** — the server now passes it as the
    `BODY` variable rather than concatenating chrome.)

11. **Composer** — `frontend/src/pages/admin/emailComposer.tsx`:
    - `useEmailTemplate(isNewsletter)` re-fetches preview chrome on toggle (add to effect
      deps; keep the `cancelled` guard so a fast toggle doesn't apply a stale template).
    - The preview keeps showing `header`/`footer` around the editor for WYSIWYG, but
      `performSend` now sends **just `body`** (not `header + body + footer`), because the
      server templates it.
    - Add a **toggle switch** (reuse `role="switch"` markup from `AdminTools.tsx:210-221`
      + existing `.switch` CSS in `App.css:250`) above the Subject field, labelled
      **"Newsletter"**, with an **info icon** (`title=` tooltip per `BlogEditor.tsx` plus
      `aria-label`). Tooltip: *"Newsletters are bulk emails members can opt out of — they
      use the newsletter template and include an unsubscribe link + List-Unsubscribe
      headers (anti-spam law). Turn off for other membership emails (welcomes, account
      notices), which use the membership template with no unsubscribe link."*
    - Add `isNewsletter` + `onIsNewsletterChange` props to `ComposeForm`; pass the flag to
      `sendNewsletter`.
    - Hosts own `isNewsletter` state, **default `true`** for both `EmailCompose.tsx` and
      `MemberEmailModal.tsx`, passed to both `useEmailTemplate` and `ComposeForm`.

12. **History badge** — reuse existing `pill` classes:
    - `frontend/src/pages/admin/EmailList.tsx`: add a "Type" column (`isNewsletter` →
      `pill pill-warn` "Newsletter"; else `pill pill-ok` "Membership").
    - `frontend/src/pages/admin/EmailDetail.tsx`: add a "Type" field row mirroring
      "From"/"Delivery".

## Edge cases

- **Template unconfigured** — `TemplateIdFor` returns null → every path falls back to raw
  HTML send (current behavior), so nothing breaks before the template IDs are wired up.
- **Internal/admin notifications** (contact-form, leased-bed application-received) — stay
  raw via `SendAsync`, recorded `IsNewsletter = false`.
- **History fidelity** — admin history shows the content fragment, not the Resend-rendered
  chrome (we don't have the rendered HTML). Acceptable; revisit if full HTML is wanted
  (would require calling Resend's render API).
- **Test emails** — unchanged, not recorded.
- **Existing rows** — `IsNewsletter = false` via migration default.

## Verification

1. `dotnet build CommonGround.slnx` + `dotnet test CommonGround.Server.Tests`.
2. Wire both template IDs. Toggle in the editor swaps the preview chrome; defaults to **on**.
3. Newsletter send → Resend receives a `Template` payload with `BODY` +
   `RESEND_UNSUBSCRIBE_URL` variables, message carries `List-Unsubscribe` headers;
   delivered mail shows the newsletter chrome + working unsubscribe; history badge
   **Newsletter**.
4. Composer toggle **off** → membership template, no unsubscribe variable/headers; history
   badge **Membership**.
5. Trigger welcome / password reset / bed assignment → delivered via the membership
   template (no raw doc), recorded `IsNewsletter = false`.
6. Contact form + leased-bed application-received → still raw to the admin address, no
   template.

## Reuse notes

- `TemplateRetrieveAsync` / `GetEmailTemplateEndpoint` — existing retrieval flow, now
  parameterized by the flag (preview only).
- `UnsubscribeTokenService` — existing token build/decode.
- Existing `.switch` / `.switch-thumb` CSS + the `role="switch"` markup in
  `AdminTools.tsx:210-221` — the toggle, no new component or CSS.
- Existing `pill` / `pill-ok` / `pill-warn` classes — the badge.
- `title=` tooltip convention (`BlogEditor.tsx`) — the info icon.
