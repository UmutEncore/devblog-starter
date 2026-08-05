---
name: seo-geo-aeo
description: >
  SEO, GEO, and AEO audit tool customized for the DevBlog repo (Angular 22 standalone frontend in frontend/devblog-ui + .NET 10 minimal-API backend in backend/). Analyzes for Search Engine Optimization (SEO), Generative Engine Optimization (GEO — AI-powered search like Perplexity, ChatGPT Search, Gemini), and Answer Engine Optimization (AEO — featured snippets, voice search). Default mode audits this repo's routes and components directly from source (app.routes.ts + each loadComponent target, index.html, services, DTO contract) — no live URL required. Also supports the original live-URL crawl mode when the user gives an external domain. Trigger on "audit my site/app", "check SEO", "her component ve route için analiz yap", "is DevBlog SEO/GEO/AEO ready", schema markup / meta tag / AI search visibility questions, or any request to review this app's search/answer-engine readiness.
---

# SEO / GEO / AEO Audit Skill — DevBlog edition

You are an expert digital marketing analyst specializing in SEO, GEO, and AEO. For this repo, your primary job is to audit the DevBlog Angular app **route by route and component by component** by reading its source, not by crawling a live URL — the repo has no deployed site and no dev server is guaranteed to be running. You still support auditing an external live URL if the user explicitly gives one (Mode B, the original workflow).

---

## Step 0: Pick the mode

- **Mode A — Repo audit (default).** The user is asking about this app, this codebase, "the frontend," specific routes/components, or gives no URL at all. Audit `frontend/devblog-ui` (and the backend contract it depends on) directly from source.
- **Mode B — Live URL audit.** The user explicitly supplies an external URL/domain to crawl (their own deployed instance, a competitor, or any other site). Use the original fetch-based workflow in [Mode B details](#mode-b-live-url-audit-original-workflow) below.

If ambiguous, default to Mode A — that's what this skill lives in this repo to do.

---

## Step 1: Confirm scope with the user

**Do not start reading files yet. Stop and ask this question first, every single time:**

> "Would you like a **Quick Audit** (top priority issues and scores — checks the route/component checklist) or a **Full Audit** (comprehensive analysis across all dimensions, plus a downloadable report)?"

Skip this only if the user's message already makes the choice unambiguous (e.g. "do a full audit" or "quick check please").

---

## Step 2 (Mode A): Build the route & component inventory from source

Never assume a signal is missing without having actually read the file that would contain it. Read, don't guess.

### 2a. Enumerate every route

Read `frontend/devblog-ui/src/app/app.routes.ts`. For each route entry, resolve the `loadComponent`/`component` target and note: path, redirect behavior (`redirectTo`/`pathMatch`), and the component file it lazy-loads. As of the current routing table this is:

| Route | Component |
|---|---|
| `''` (redirect) | → `posts` |
| `posts` | `pages/post-list/post-list.component.ts` |
| `posts/:slug` | `pages/post-detail/post-detail.component.ts` |
| `login` | `pages/login/login.component.ts` |

Re-derive this table at analysis time rather than trusting it verbatim — routes get added/removed. If new routes exist, read them too.

### 2b. Read every component, its template, and shared shell files

For **each** routed component, read both the `.component.ts` and its `.component.html` (check for an inline `template` too). Also read the app-wide files that affect every route because this is a single-page, single-`index.html` app:

- `frontend/devblog-ui/src/index.html` — the one static `<title>`/meta block served to every route
- `frontend/devblog-ui/src/app/app.component.ts` — shell/nav markup
- `frontend/devblog-ui/src/app/app.config.ts` — check whether `@angular/ssr`/hybrid rendering, `Title`, or `Meta` providers are registered
- `frontend/devblog-ui/src/app/services/post.service.ts` and `auth.service.ts` — the DTO/interface contracts that shape what content each page *could* render
- `frontend/devblog-ui/package.json` and `angular.json` — grep for `@angular/ssr`, `prerender`, `ssr` to check the app's rendering mode
- `frontend/devblog-ui/public/` (or project root) — check for `robots.txt`, `sitemap.xml`, `favicon`, a web app manifest

Grep for `Title`/`Meta` imports from `@angular/platform-browser` across `frontend/devblog-ui/src` to see if any route manages its own `<title>`/meta tags — in a CSR SPA with no such usage, every route shares the exact same `<title>` and has zero meta description, which is a top-line finding, not a per-route one.

### 2c. Optional live supplement

If the user has a dev server running (or says so), you may supplement with a live check: `WebFetch` on `http://localhost:4200/<route>` (or whatever `environment.ts`/`environment.development.ts` shows as `apiUrl`'s counterpart dev-server port) to see the actual rendered/initial HTML a crawler would get. Treat connection failure as expected and non-fatal — fall back to the static source read, don't block the audit on it.

### 2d. Backend contract check

Read `backend/src/DevBlog.Api/Endpoints/PostsEndpoint.cs`, `Services/PostService.cs`, `Models/Post.cs`, `Models/User.cs`, `Models/Comment.cs`, and `Data/DataSeeder.cs` to check whether the API even carries fields the frontend would need for good SEO/GEO/AEO — e.g. an excerpt/meta-description field, an OG/cover image URL, an `updatedAt` freshness field, author credentials/bio. Per `CLAUDE.md`, the frontend's TS interfaces and the backend's DTOs are manually kept in sync with no shared schema — flag any gap on either side as a two-file fix (`post.service.ts` interface + the DTO record), not a frontend-only one.

---

## Baseline analysis (component × route matrix)

This is the result of actually reading every routed component, its template, the app shell, and the backend contract on 2026-08-05 — a worked example of what Step 2/3 above should produce, not a substitute for redoing it. **Treat every cell as a claim about the code as of that date — re-read the cited file before repeating a finding**; components, DTOs, and `index.html` all change over time and this table will drift.

| Route | Component / template | SEO finding | GEO finding | AEO finding |
|---|---|---|---|---|
| `''` → `posts` | `app.routes.ts:4` (`redirectTo`, `pathMatch: 'full'`) | Client-side redirect only — a crawler hitting `/` gets whatever `index.html` serves, then JS navigates; no HTTP 3xx. | Same JS-dependent redirect blocks non-JS AI crawlers from ever reaching `/posts`. | N/A |
| `posts` | `post-list.component.ts` + `.html` | Single `<h1>Posts</h1>` ([post-list.component.html:1](frontend/devblog-ui/src/app/pages/post-list/post-list.component.html#L1)); list items link post titles to `/posts/:slug` (good anchor text); no intro copy, no pagination (`PostsEndpoint.cs:10-12` returns all posts unpaginated — flagged tech debt in `CLAUDE.md` already). | Author shown as bare username string (`PostSummaryDto.AuthorUsername` from `PostService.cs:13`) — no bio/entity link. | No question-phrased content; it's an index, not answer content — fine as-is. |
| `posts/:slug` | `post-detail.component.ts` + `.html` | `<h1>{{post.title}}</h1>` ([post-detail.component.html:3](frontend/devblog-ui/src/app/pages/post-detail/post-detail.component.html#L3)) — good, unique per post. `post.content` renders as one interpolated string in a single `<div>` ([post-detail.component.html:5](frontend/devblog-ui/src/app/pages/post-detail/post-detail.component.html#L5)) — no subheadings/paragraphs/lists. Seeded content (`DataSeeder.cs:28-59`) is ~45-60 words per post, one paragraph each — well under useful depth. No JSON-LD anywhere in the template. | `Author` model (`Models/User.cs`) has only `Username`/`PasswordHash`/`Role` — no display name, bio, or credentials to show even if the template wanted to. `PostDetailDto` (`PostService.cs:21-24`) has `Title, Content, Slug, Tags, PublishedAt, AuthorUsername, Comments` — no `Excerpt`, `UpdatedAt`, or cover-image field to hang a meta description/OG image on. | Comments section uses literal headings `<h2>Comments</h2>` / `<h3>Add a comment</h3>` ([post-detail.component.html:9](frontend/devblog-ui/src/app/pages/post-detail/post-detail.component.html#L9),[17](frontend/devblog-ui/src/app/pages/post-detail/post-detail.component.html#L17)) — not question-phrased, not AEO targets; the article body itself has no internal subheadings to turn into snippet/PAA targets. |
| `login` | `login.component.ts` + `.html` | Minimal by design — `<h1>Login</h1>`, a form. No content depth expected. | N/A | N/A — but note under Step 3: no mechanism exists to `noindex` it even if desired. |
| *(all routes)* | `index.html` | One static `<title>DevBlog</title>` ([index.html:5](frontend/devblog-ui/src/index.html#L5)), viewport meta present ([index.html:7](frontend/devblog-ui/src/index.html#L7)), **no** meta description, canonical, OG/Twitter tags, or JSON-LD. No `Title`/`Meta` service import found anywhere under `frontend/devblog-ui/src` (grepped) — confirms every route shares this exact markup. | `app.component.ts` shell has only a two-link `<nav>` (Posts \| Login) — no site description, no Organization identity beyond the `<title>`. | — |
| *(build/infra)* | `package.json`, `angular.json` | No `@angular/ssr` dependency, no `prerender`/`ssr`/`outputMode`/`server` keys in `angular.json` — this is CSR-only, no hybrid rendering. No `public/` folder anywhere in the project — no `robots.txt`, `sitemap.xml`, or favicon. | CSR-only + no static crawl map compounds the entity-recognition problem: nothing tells a crawler what pages exist besides following in-app links. | — |

**What this baseline changes about the generic checklist below**: don't re-ask "is there a meta description" three times — it's one app-wide finding (the `index.html` row). Don't recommend "add a Team/author bio page" as a copy change — it requires a `User.cs` schema change first (`DisplayName`/bio fields), then a DTO change, then a template change — call out the full chain. Don't score AEO purely on missing FAQ schema — the more concrete, fixable gap here is that `post.content` has zero internal structure to *become* a snippet target in the first place.

---

## 2026 context: what actually moves AI visibility (read before scoring)

Google published its first dedicated generative-AI-search guide on 2026-05-15
(developers.google.com/search/docs/fundamentals/ai-optimization-guide), stating plainly
that **AEO and GEO are "still SEO"** for Google's own AI Overviews/AI Mode — they're
powered by the same core Search ranking and quality systems, not a separate discipline.
This reprioritizes several items below:

- **Structured data (JSON-LD) is explicitly *not required* for AI Overviews/AI Mode.**
  A May 2026 study (reported by Search Engine Journal) found adding JSON-LD produced no
  measurable increase in AI citations for pages already visible in AI Overviews. Still
  recommend `BlogPosting`/`Article` schema as a **classic-SEO / rich-results** win (it's
  cheap and can still earn rich snippets in regular Search), but do not score or prioritize
  it as a GEO/AEO lever — that overstates its effect.
- **No `llms.txt`, no content-chunking, no rewriting for "AI algorithms."** Google
  explicitly says these aren't needed — Google Search ignores `llms.txt`-style files, and
  its systems "understand the nuance of multiple topics on a page" without content being
  pre-chunked. Don't recommend adding one; if the user asks, cite this directly.
- **What Google says actually works**: unique, non-commodity content with a genuine point
  of view; crawlable/indexable technical structure; good page experience; the same
  fundamentals that earn organic rankings earn AI citations too. This validates prioritizing
  this skill's core-content and crawlability findings over schema/markup findings.
- **This guidance is Google-specific — other AI answer engines are not the same crawler.**
  ChatGPT Search (`OAI-SearchBot`), Perplexity (`PerplexityBot`), and Claude's answer
  surface (`Claude-SearchBot`) run their own independent crawlers/indexes, separate from
  Google's. Unlike Googlebot, most of these **do not execute JavaScript** — so a CSR-only
  Angular app (see the rendering-mode finding below) can look empty to them even on a page
  Google itself indexes and cites fine. Don't let "Google says schema/llms.txt don't
  matter" imply the CSR-rendering gap doesn't matter — for non-Google AI surfaces, it's
  still the single biggest blocker this app has.
- **If/when a `robots.txt` is added** (this repo currently has none — see the baseline
  table), decide per AI-bot category rather than one blanket allow/disallow: training
  crawlers (`GPTBot`, `ClaudeBot`, `Google-Extended`) feed model training; real-time search
  crawlers (`OAI-SearchBot`, `PerplexityBot`, `Claude-SearchBot`) drive live AI-answer
  citations; user-triggered agents (`ChatGPT-User`, `Claude-User`) fetch a page only when a
  user explicitly asks their assistant to visit it. A site can block training crawlers while
  staying eligible for AI-answer citations by allowing only the search/user-agent bots —
  surface this as an explicit choice for the user to make, don't default to blocking or
  allowing everything.

Re-verify this section's claims (especially the "not required" points) against Google's
current published guidance before repeating them verbatim in an audit — Google's stated
position on AI search has already changed once (this guide didn't exist before May 2026)
and may change again.

---

## Step 3: Analyze the signals

Analyze **per route**, then roll up. Because this app is CSR-only with one shared `index.html`, most Technical On-Page and structured-data signals are identical across all three routes by construction — say so once, don't repeat "missing meta description" three separate times as if they were independent findings on each page. Content-quality and AEO signals (heading structure, content depth, question coverage) genuinely differ per route, so assess those individually.

### SEO Signals

**Technical On-Page (app-wide, driven by `index.html` + rendering mode):**
- **Title tag**: present in `index.html`? Does it change per route (requires `Title` service usage — check for it)? Length/keyword relevance if static.
- **Meta description**: present at all? Per-route or absent entirely?
- **Canonical tag / robots meta**: present? Would `/login` benefit from `noindex` given it has no content value?
- **Viewport meta**: present (check `index.html`)?
- **Open Graph / Twitter Card**: any `og:*`/`twitter:*` tags, static or dynamic?
- **Rendering mode**: is this CSR-only (`@angular/platform-browser-dynamic`, no `@angular/ssr` in `package.json`)? A pure-CSR app with no prerendering means the HTML a non-JS-executing crawler fetches is effectively empty — this is usually the single highest-leverage finding for the whole app, not a per-page one.
- **`robots.txt` / `sitemap.xml`**: present under `public/` (or wherever Angular serves static assets)?

**Per-route Content Quality:**
- **`posts` (list)**: H1 present (`<h1>Posts</h1>`)? Content beyond the list itself (intro copy, categories)? Internal link anchor text quality (post titles as link text — good practice already)?
- **`posts/:slug` (detail)**: H1 present and equal to the post title? Is `post.content` rendered as structured HTML (paragraphs, subheadings) or as one opaque string in a single `<div>`? Word count depends on seeded content — check actual seeded posts via `DataSeeder.cs` rather than assuming. Freshness (`publishedAt`) shown — is there an `updatedAt`?
- **`login`**: intentionally thin — correct to have minimal content here; the SEO question is whether it should be excluded from indexing at all, not whether it needs more content.
- **Image alt text**: are there any `<img>` tags anywhere in the templates today? If none exist, note alt-text as "not yet applicable" rather than "missing," and flag it as a requirement for whenever images are added (e.g. author avatars, cover images).

**Structured Data:**
- **Schema markup**: search all templates/components for JSON-LD (`<script type="application/ld+json">`) or Angular-rendered schema. Expect `BlogPosting`/`Article` on `posts/:slug`, `Organization`/`WebSite` app-wide, `BreadcrumbList` on `posts/:slug`. Per the [2026 context](#2026-context-what-actually-moves-ai-visibility-read-before-scoring) note above, score its absence as a **classic-SEO rich-results gap**, not a GEO/AEO one — Google states this isn't required for AI Overviews/AI Mode, and a 2026 study found no measurable AI-citation lift from adding it to pages already visible in AI Overviews.

### GEO Signals

**E-E-A-T:**
- **Author information**: `post.author` is rendered as a plain string on list and detail pages — is there any author profile, bio, or credentials anywhere (a dedicated page, a linked author entity)? Check `PostSummaryDto`/`PostDetailDto` for anything beyond a name string.
- **About/organization context**: does the app state anywhere what DevBlog is or who runs it? (Check `app.component.ts` shell and any static page.)
- **Trust signals**: none expected in a starter blog — note as N/A rather than penalizing a demo app for lacking testimonials/press mentions.
- **Organization schema**: absent unless added (see Structured Data above).

**Content for AI Synthesis:**
- **Factual density / comprehensiveness**: depends entirely on the actual seeded/authored post content (`DataSeeder.cs` or real posts) — read it, don't assume.
- **Entity clarity**: is "DevBlog" used consistently as the brand name across `index.html` title, nav, and any footer?

**Technical GEO:**
- **Clean crawlability**: CSR-only rendering (see above) is the dominant technical GEO gap for **non-Google** AI answer engines — `GPTBot`/`OAI-SearchBot` (ChatGPT), `PerplexityBot`, and `Claude-SearchBot`/`ClaudeBot` run their own crawlers and, unlike Googlebot, generally do not execute JavaScript, so they'll see empty content on every route. Googlebot itself renders JS, so this gap is less severe for Google's own AI Overviews specifically — call out both halves rather than treating "AI crawlers" as one undifferentiated group.
- **`robots.txt` AI-bot policy**: this repo has no `robots.txt` at all (see baseline table) — there's nothing to evaluate yet, but flag it as a decision point: see the per-bot-category guidance in [2026 context](#2026-context-what-actually-moves-ai-visibility-read-before-scoring) above for how to advise the user once one is added.
- **HTTPS**: not assessable from source; note this is a deployment-time concern, and to verify via the deployed URL once one exists.

### AEO Signals

**Featured Snippet / structured answers:**
- **Definition patterns / direct answers**: blog posts rarely need this, but check if any post content answers a question directly near the top.
- **FAQ/HowTo schema**: none expected today — flag as a future opportunity if the content strategy calls for how-to posts.
- **Question-phrased headings**: `posts/:slug` only has `<h2>Comments</h2>` and `<h3>Add a comment</h3>` — neither is question-phrased nor AEO-relevant; note this is fine for a comments section but means the page has no AEO-targeting subheadings within the article body itself (because `post.content` isn't broken into subheadings at all — it's one interpolated string).
- **Voice search / long-tail coverage**: depends on actual post content, same caveat as above.

---

## Step 4: Score rubric

Score each dimension 1-10:
- **1-3**: Critical issues — effectively invisible to search/AI engines
- **4-5**: Below average — significant missed opportunities
- **6-7**: Decent foundation — specific improvements needed
- **8-9**: Strong — minor refinements available
- **10**: Exemplary

Keep the in-chat response brief. Use this format for both modes:

---

## 🔍 DevBlog — [Quick/Full] SEO/GEO/AEO Audit

**Routes reviewed:** [count and list, e.g. `/posts`, `/posts/:slug`, `/login`]  **Audit date:** [date]

| Dimension | Score | Status |
|---|---|---|
| SEO | X/10 | [Needs Work / On Track / Strong] |
| GEO | X/10 | [Needs Work / On Track / Strong] |
| AEO | X/10 | [Needs Work / On Track / Strong] |

**Top 3 priorities:** [One sentence each, naming the specific file/route to fix — e.g. "Add `Title`/`Meta` service calls in `post-detail.component.ts` so each post gets a unique `<title>`/description."]

**Biggest strength:** [One sentence — something genuinely working, e.g. clean slug-based URLs, unique-slug DB constraint already enforced.]

*Full findings and the recommendations matrix are in the report below (Full Audit) or listed in chat (Quick Audit).*

---

For a **Quick Audit**, stop here (plus a short bullet list of the top issues per route) — no report generation.

## Step 5 (Full Audit only): Generate the downloadable report

Tell the user: "Generating your downloadable report now..."

### Setup

Check whether `docx` is already available before installing, in one combined command:

```bash
node -e "require('docx')" 2>/dev/null || npm install -g docx
```

This environment has no bundled sandbox `docx`/`soffice` skill scripts and no guaranteed LibreOffice install — do not reference `/sessions/.../mnt/...` paths, they don't exist here. Instead:

- Write the `.docx` to the session scratchpad directory given in your system prompt (or, if the user wants it kept permanently in the repo, a `docs/audits/` folder — ask if unclear).
- Before attempting PDF conversion, check for a LibreOffice binary: `where soffice` (PowerShell) or `command -v soffice` (bash). If found, run `soffice --headless --convert-to pdf <file> --outdir <dir>`. If not found, **skip PDF conversion** and tell the user the `.docx` is ready and that PDF export needs LibreOffice or opening/exporting from Word — don't fail the whole step over a missing converter.

Write and run the full report-generation script in one shot once `docx` is confirmed available.

### Report design

**Color palette:**
- Navy header/cover: `1B2A4A`
- Accent blue: `2563EB`
- Score green (8-10): `16A34A`
- Score amber (5-7): `D97706`
- Score red (1-4): `DC2626`
- Light gray background for alternating table rows: `F8F9FA`
- Medium gray for borders: `E2E8F0`
- Dark text: `1E293B`
- Light section background: `EFF6FF`

**Typography:** Arial throughout. Title 36pt bold, H1 24pt bold, H2 18pt bold, H3 14pt bold, body 11pt, footer 9pt.

**Page setup:** US Letter (12240 x 15840 DXA), 1-inch margins. Content width: 9360 DXA.

### Report structure

#### 1. Cover page (separate section, no header/footer)

Full-page navy background (`1B2A4A`), content vertically centered via spacer paragraphs:
1. "DevBlog" in white, 36pt bold — hero element (Mode B: the domain instead)
2. "SEO / GEO / AEO Audit Report" in light blue (`93C5FD`), 18pt
3. "QUICK AUDIT" or "FULL AUDIT" in white, 11pt
4. Score table — 3 columns, full width, no visible outer border; each cell colored by score band (green/amber/red), showing dimension label, score number (36pt bold), status word

Bottom: audit date + "Repo: devblog-starter" in gray (`94A3B8`), 9pt, centered. Page break after cover.

#### 2. Executive summary

Heading 1. Light-blue shaded box (`EFF6FF`, single-cell table) with 3-5 sentences specific to what was actually found in this codebase — not generic filler. Below it, the scores table (SEO/GEO/AEO/Combined) with color-coded Score cells.

#### 3. Routes & components audited

Heading 1. Table: **Route | Component (file path) | Rendering | Notes** — e.g. `posts/:slug | pages/post-detail/post-detail.component.ts | CSR, no per-route meta | Article content rendered as single unstyled string`. Alternating row shading.

#### 4. SEO analysis

Heading 1, with score subtitle. Sub-sections (Heading 2): Technical On-Page, Content Quality (per route), Structured Data. Each finding as a 3-column table: Signal | Finding | Status (color-coded: Good/Needs Attention/Missing).

#### 5. GEO analysis

Same structure. Sub-sections: E-E-A-T Assessment, Content for AI Synthesis, Technical GEO.

#### 6. AEO analysis

Same structure. Sub-sections: Featured Snippet Eligibility, Structured Answer Formats, Voice Search Readiness.

#### 7. Priority recommendations matrix

Heading 1. Full-width, 5 columns: Priority | Issue | Dimension | Effort | Impact. Every "Issue" must name a concrete file/route to change (e.g. "Register `provideClientHydration`/`@angular/ssr` in `app.config.ts`" or "Add `excerpt` field to `PostSummaryDto` and `PostSummary` interface"). Color-code Priority: 🔴 Critical (`DC2626`), 🟠 High (`EA580C`), 🟡 Medium (`D97706`), 🟢 Quick Win (`16A34A`) — all white text.

#### 8. What's working well

Heading 1. Green-tinted table (`F0FDF4`) — genuine strengths with file/route evidence (e.g. slug-based URLs, DB-enforced unique slugs, standalone lazy-loaded routes keeping bundles small).

#### 9. Glossary (Full Audit only)

Brief plain-English definitions of SEO, GEO, AEO.

### Headers/footers (all pages except cover)

**Header:** "DevBlog" left, "SEO / GEO / AEO Audit Report" right, navy bottom border.
**Footer:** page number right, gray top border.

### Generate, validate, deliver

```javascript
const { Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
        Header, Footer, AlignmentType, HeadingLevel, BorderStyle, WidthType,
        ShadingType, VerticalAlign, PageNumber, PageBreak } = require('docx');
const fs = require('fs');

// ... build document as described above ...

Packer.toBuffer(doc).then(buffer => {
  fs.writeFileSync('<scratchpad-or-docs-dir>/seo-audit-devblog-[date].docx', buffer);
  console.log('DOCX written');
});
```

If the write throws or the file looks truncated, inspect the error and fix the script rather than silently producing a partial file. Deliver the resulting path(s) to the user as plain file paths (this is a local CLI environment, not a hosted sandbox — no `computer://` links).

---

## Step 6: Invite next steps

> "Want me to go deeper on any route, wire up one of these fixes directly (e.g. add `Title`/`Meta` service calls, register `@angular/ssr` so non-JS AI crawlers see real content), or re-run this after changes land?"

---

## Important principles (both modes)

**Never flag something as missing without having read the file that would contain it.** For Mode A that means actually opening `index.html`, each component, `app.config.ts`, etc. — not inferring from the file list.

**Distinguish app-wide findings from per-route findings.** In a single-`index.html` CSR app, don't repeat the same "no meta description" finding three times as if independent — state it once at the app level and note it therefore applies to every route.

**Be specific, not generic.** Reference actual code: quote the real `<title>` text, name the exact component/DTO to change, cite the actual route path.

**Be honest about what source-reading can't tell you.** Core Web Vitals, real rendering performance, actual crawler behavior, and backlink/domain authority need a live, deployed URL and external tools (PageSpeed Insights, Search Console) — say so rather than guessing, and re-check with a live crawl (Mode B) once the app is deployed.

**Don't penalize a starter/demo app for things it was never trying to be.** `login` doesn't need content depth; a lack of testimonials/press isn't a finding for a blog starter. Calibrate severity to what the app is.

---

## Mode B: Live URL audit (original workflow)

Use this only when the user gives an explicit external URL to crawl (their deployed instance, a competitor, or an unrelated site). The [2026 context](#2026-context-what-actually-moves-ai-visibility-read-before-scoring) note above applies here too — don't overweight schema markup/`llms.txt` findings relative to core content quality and JS-rendering/crawlability, and separate "renders fine for Googlebot" from "renders fine for GPTBot/PerplexityBot/Claude-SearchBot" when judging Technical GEO.

### Fetch and collect data

Use WebFetch to gather page data. Never assume a site does or doesn't have something until you've looked.

**Homepage fetch and site discovery**: fetch the URL, extract nav/footer/internal links, build a page map (About, Team, Services, Case Studies, Blog, FAQ, Contact, etc.). Also fetch `{domain}/robots.txt` and `{domain}/sitemap.xml` in parallel.

**Crawl key pages**: About/Team, Services/Work, Case Studies/Portfolio, Blog/Resources (index + individual posts), Contact, FAQ. Quick Audit: homepage + up to 6 pages. Full Audit: crawl as many meaningful pages as exist, skipping only Privacy/Terms/login/thank-you/deep pagination.

**Inaccessible sites**: if the primary URL fails, tell the user, confirm it's publicly reachable, and offer a framework audit in the meantime. If secondary pages fail individually, note it and continue.

### Analyze the signals

Same three dimension categories as Mode A (SEO/GEO/AEO), but applied per fetched page instead of per component — see the generic signal checklist:

**SEO** — Technical On-Page (title, meta description, heading hierarchy, URL structure, canonical, robots meta, viewport, alt text, internal links, OG/Twitter cards), Content Quality (word count, keyword signals, freshness, readability), Structured Data (schema types + validity).

**GEO** — E-E-A-T (author info, About page, contact info, trust signals, Organization schema), Content for AI Synthesis (factual density, clear claims, source citation, comprehensiveness, entity clarity, originality), Technical GEO (structured data depth, HTTPS, crawlability, sameAs links).

**AEO** — Featured Snippet Eligibility (direct-answer paragraphs, definitions, lists, tables), Structured Answer Formats (FAQ/HowTo/Speakable schema, question-phrased headings), Voice Search Readiness (conversational language, long-tail coverage, local/NAP signals).

Then proceed through Steps 4-6 exactly as above, substituting "site/page" for "app/route" and the live domain for "DevBlog" throughout the report (cover page, headers/footers, filename).
