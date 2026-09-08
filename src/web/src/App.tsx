import { useState, useMemo, useRef, useEffect } from "react";
import { useMsal } from "@azure/msal-react";
import { apiRequest, authConfigured } from "@/auth/authConfig";
import { fetchMe } from "@/auth/me";
import {
  spaces as allSpaces,
  forms as allForms,
  submissions,
  userSubmissions,
  userAccessibleForms,
} from "@/data/mockData";
import type { Form, AppUser, Submission } from "@/data/mockData";
import AdminPortal from "@/components/AdminPortal";
import FormRenderer from "@/components/FormRenderer";
import { spaceColor, formatDate } from "@/utils/helpers";

// ─── Types ───────────────────────────────────────────────────────────────────

type NavItem = "home" | "my-forms" | "submissions" | "management" | "about";
type AuthState = "none" | "user" | "admin";

// ─── Icons ───────────────────────────────────────────────────────────────────

function IconHome() {
  return (
    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 12l8.954-8.955a1.126 1.126 0 011.591 0L21.75 12M4.5 9.75v10.125c0 .621.504 1.125 1.125 1.125H9.75v-4.875c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21h4.125c.621 0 1.125-.504 1.125-1.125V9.75M8.25 21h8.25" />
    </svg>
  );
}

function IconForms() {
  return (
    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
    </svg>
  );
}

function IconSubmissions() {
  return (
    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 9.776c.112-.017.227-.026.344-.026h15.812c.117 0 .232.009.344.026m-16.5 0a2.25 2.25 0 00-1.883 2.542l.857 6a2.25 2.25 0 002.227 1.932H19.05a2.25 2.25 0 002.227-1.932l.857-6a2.25 2.25 0 00-1.883-2.542m-16.5 0V6A2.25 2.25 0 016 3.75h3.879a1.5 1.5 0 011.06.44l2.122 2.12a1.5 1.5 0 001.06.44H18A2.25 2.25 0 0120.25 9v.776" />
    </svg>
  );
}

function IconManagement() {
  return (
    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M10.343 3.94c.09-.542.56-.94 1.11-.94h1.093c.55 0 1.02.398 1.11.94l.149.894c.07.424.384.764.78.93.398.164.855.142 1.205-.108l.737-.527a1.125 1.125 0 011.45.12l.773.774c.39.389.44 1.002.12 1.45l-.527.737c-.25.35-.272.806-.107 1.204.165.397.505.71.93.78l.893.15c.543.09.94.56.94 1.109v1.094c0 .55-.397 1.02-.94 1.11l-.893.149c-.425.07-.765.383-.93.78-.165.398-.143.854.107 1.204l.527.738c.32.447.269 1.06-.12 1.45l-.774.773a1.125 1.125 0 01-1.449.12l-.738-.527c-.35-.25-.806-.272-1.203-.107-.397.165-.71.505-.781.929l-.149.894c-.09.542-.56.94-1.11.94h-1.094c-.55 0-1.019-.398-1.11-.94l-.148-.894c-.071-.424-.384-.764-.781-.93-.398-.164-.854-.142-1.204.108l-.738.527c-.447.32-1.06.269-1.45-.12l-.773-.774a1.125 1.125 0 01-.12-1.45l.527-.737c.25-.35.273-.806.108-1.204-.165-.397-.505-.71-.93-.78l-.894-.15c-.542-.09-.94-.56-.94-1.109v-1.094c0-.55.398-1.02.94-1.11l.894-.149c.424-.07.765-.383.93-.78.165-.398.143-.854-.107-1.204l-.527-.738a1.125 1.125 0 01.12-1.45l.773-.773a1.125 1.125 0 011.45-.12l.737.527c.35.25.807.272 1.204.107.397-.165.71-.505.78-.929l.15-.894z" />
      <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
    </svg>
  );
}

function IconAbout() {
  return (
    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" />
    </svg>
  );
}

function IconSearch() {
  return (
    <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
    </svg>
  );
}

function LogoMark() {
  return (
    <svg className="w-5 h-5 flex-shrink-0" viewBox="0 0 16 16" fill="none">
      <rect width="7" height="7" rx="1.5" fill="#3d7fff" />
      <rect x="9" width="7" height="7" rx="1.5" fill="#3d7fff" opacity="0.5" />
      <rect y="9" width="7" height="7" rx="1.5" fill="#3d7fff" opacity="0.5" />
      <rect x="9" y="9" width="7" height="7" rx="1.5" fill="#3d7fff" opacity="0.25" />
    </svg>
  );
}

// ─── Session helpers (real Entra sign-in) ────────────────────────────────────

function initialsOf(name: string): string {
  return (
    name
      .trim()
      .split(/\s+/)
      .map((n) => n[0])
      .join("")
      .slice(0, 2)
      .toUpperCase() || "?"
  );
}

function SessionChecking() {
  return (
    <div className="h-full flex items-center justify-center bg-background text-foreground p-6">
      <p className="text-xs text-muted-foreground animate-pulse">Checking your session…</p>
    </div>
  );
}

function SessionError({
  message,
  onRetry,
  onSignOut,
}: {
  message: string;
  onRetry: () => void;
  onSignOut: () => void;
}) {
  return (
    <div className="h-full flex items-center justify-center bg-background text-foreground p-6">
      <div className="max-w-sm bg-card border border-border rounded-lg p-6 mx-4">
        <h1 className="text-sm font-semibold mb-2">Couldn't verify your sign-in</h1>
        <p className="text-xs text-muted-foreground leading-relaxed mb-4">{message}</p>
        <div className="flex gap-2">
          <button
            className="px-3 py-1.5 bg-primary text-primary-foreground text-xs font-semibold rounded hover:bg-primary/90 active:scale-[0.98] transition-all"
            onClick={onRetry}
          >
            Try again
          </button>
          <button
            className="px-3 py-1.5 bg-secondary border border-border text-xs font-semibold rounded hover:bg-secondary/80 active:scale-[0.98] transition-all"
            onClick={onSignOut}
          >
            Sign out
          </button>
        </div>
      </div>
    </div>
  );
}

// ─── Home View ───────────────────────────────────────────────────────────────

function HomeView({
  auth,
  user,
  onOpenForm,
  onLogin,
}: {
  auth: AuthState;
  user: AppUser | null;
  onOpenForm: (form: Form) => void;
  onLogin: () => void;
}) {
  const visibleSpaces = auth === "none" ? [] : allSpaces;
  const visibleForms =
    auth === "admin"
      ? allForms
      : auth === "user"
      ? userAccessibleForms
      : [];

  return (
    <div className="flex-1 overflow-auto p-6">
      {/* Welcome header */}
      <div className="mb-7">
        <h1 className="text-base font-semibold text-foreground">
          {auth === "none" ? "Welcome to FormCraft" : `Welcome back, ${user?.name.split(" ")[0]}`}
        </h1>
        <p className="text-xs text-muted-foreground mt-1">
          {auth === "none"
            ? "Dynamic form configuration and submission platform for enterprise teams."
            : auth === "admin"
            ? "All spaces and forms across the organisation."
            : "Spaces and forms you are authorised to access."}
        </p>
      </div>

      {auth === "none" ? (
        /* Not logged in — system info + login CTA */
        <div className="grid gap-5 max-w-2xl">
          <div className="bg-card border border-border rounded-lg p-5">
            <h2 className="text-sm font-semibold text-foreground mb-2">What is FormCraft?</h2>
            <p className="text-xs text-muted-foreground leading-relaxed">
              FormCraft is an enterprise-grade form platform. Admins design JSON Schema–backed forms with a
              live visual editor; users fill and submit forms through a clean, guided interface. All
              submissions are searchable, filterable, and exportable as Excel or JSON.
            </p>
          </div>
          <div className="grid grid-cols-3 gap-3">
            {[
              { label: "Spaces", value: allSpaces.length },
              { label: "Forms", value: allForms.length },
              { label: "Submissions", value: submissions.length },
            ].map(({ label, value }) => (
              <div key={label} className="bg-card border border-border rounded-lg p-4 text-center">
                <div className="text-2xl font-bold text-foreground font-mono">{value}</div>
                <div className="text-[11px] text-muted-foreground mt-1">{label}</div>
              </div>
            ))}
          </div>
          <div>
            <button
              className="px-5 py-2.5 bg-primary text-primary-foreground text-sm font-semibold rounded-lg hover:bg-primary/90 active:scale-[0.98] transition-all"
              onClick={onLogin}
            >
              Sign In
            </button>
          </div>
        </div>
      ) : (
        /* Logged in — spaces with accessible forms */
        <div className="flex flex-col gap-6">
          {visibleSpaces.map((space) => {
            const spaceForms = visibleForms.filter((f) => f.spaceId === space.id);
            if (spaceForms.length === 0) return null;
            return (
              <div key={space.id}>
                <div className="flex items-center gap-2.5 mb-3">
                  <div className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: space.color }} />
                  <h2 className="text-xs font-bold tracking-wide uppercase text-muted-foreground">{space.name}</h2>
                  <div className="flex-1 h-px bg-border/60" />
                  <span className="text-[10px] text-muted-foreground font-mono">{spaceForms.length} form{spaceForms.length !== 1 ? "s" : ""}</span>
                </div>
                <div
                  className="grid gap-3"
                  style={{ gridTemplateColumns: "repeat(auto-fill, minmax(240px, 1fr))" }}
                >
                  {spaceForms.map((form) => {
                    const myCount = (auth === "user" ? userSubmissions : submissions).filter(
                      (s) => s.formId === form.id,
                    ).length;
                    return (
                      <div
                        key={form.id}
                        className="bg-card border border-border rounded-lg p-4 flex flex-col gap-3 hover:border-primary/30 transition-colors"
                      >
                        <div className="flex items-start justify-between gap-2">
                          <h3 className="text-xs font-semibold text-foreground leading-snug">{form.name}</h3>
                          <div className="text-right flex-shrink-0">
                            <div className="text-lg font-bold text-foreground font-mono leading-none">
                              {auth === "admin" ? form.submissionCount : myCount}
                            </div>
                            <div className="text-[10px] text-muted-foreground">{auth === "admin" ? "total" : "mine"}</div>
                          </div>
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-[10px] text-muted-foreground">{Object.keys(form.schema.properties).length} fields</span>
                          <span className="text-[10px] text-muted-foreground font-mono">upd. {form.updatedAt}</span>
                        </div>
                        <button
                          className="mt-auto w-full py-1.5 bg-primary/10 hover:bg-primary/20 text-primary text-xs font-semibold rounded transition-colors"
                          onClick={() => onOpenForm(form)}
                        >
                          Open Form →
                        </button>
                      </div>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

// ─── My Forms View ────────────────────────────────────────────────────────────

function MyFormsView({ auth, onOpenForm }: { auth: AuthState; onOpenForm: (form: Form) => void }) {
  const forms = auth === "admin" ? allForms : userAccessibleForms;

  return (
    <div className="flex-1 overflow-auto p-6">
      <div className="mb-6">
        <h1 className="text-base font-semibold text-foreground">My Forms</h1>
        <p className="text-xs text-muted-foreground mt-0.5">
          {auth === "admin" ? "All forms across the organisation." : "Forms you are authorised to fill and submit."}
        </p>
      </div>
      <div className="grid gap-4" style={{ gridTemplateColumns: "repeat(auto-fill, minmax(260px, 1fr))" }}>
        {forms.map((form) => {
          const space = allSpaces.find((s) => s.id === form.spaceId);
          const myCount = (auth === "user" ? userSubmissions : submissions).filter(
            (s) => s.formId === form.id,
          ).length;
          return (
            <div
              key={form.id}
              className="bg-card border border-border rounded-lg p-4 flex flex-col gap-3 hover:border-primary/30 transition-colors"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <h3 className="text-sm font-semibold text-foreground leading-snug">{form.name}</h3>
                  <div className="flex items-center gap-1.5 mt-1.5">
                    <span className="w-1.5 h-1.5 rounded-full flex-shrink-0" style={{ backgroundColor: space?.color }} />
                    <span className="text-[11px] text-muted-foreground">{space?.name}</span>
                  </div>
                </div>
                <div className="text-right flex-shrink-0">
                  <div className="text-xl font-bold text-foreground font-mono leading-none">
                    {auth === "admin" ? form.submissionCount : myCount}
                  </div>
                  <div className="text-[10px] text-muted-foreground mt-0.5">
                    {auth === "admin" ? "total sub." : "my sub."}
                  </div>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-[11px] text-muted-foreground">{Object.keys(form.schema.properties).length} fields</span>
                <span className="text-[10px] text-muted-foreground font-mono">upd. {form.updatedAt}</span>
              </div>
              <button
                className="mt-auto w-full py-1.5 bg-primary/10 hover:bg-primary/20 text-primary text-xs font-semibold rounded transition-colors"
                onClick={() => onOpenForm(form)}
              >
                Open Form →
              </button>
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ─── Fill Form View ───────────────────────────────────────────────────────────

function FillFormView({ form, onBack }: { form: Form; onBack: () => void }) {
  return (
    <div className="flex-1 overflow-auto flex flex-col">
      <div className="flex items-center gap-2 px-5 py-2.5 border-b border-border text-xs bg-card/30 flex-shrink-0">
        <button className="text-muted-foreground hover:text-foreground transition-colors" onClick={onBack}>
          ← My Forms
        </button>
        <span className="text-border">/</span>
        <span className="text-foreground font-medium">{form.name}</span>
      </div>
      <div className="flex-1 overflow-auto p-6">
        <div className="max-w-2xl">
          <h2 className="text-sm font-semibold text-foreground mb-1">{form.schema.title ?? form.name}</h2>
          {form.schema.description && (
            <p className="text-xs text-muted-foreground mb-5 leading-relaxed">{form.schema.description}</p>
          )}
          <FormRenderer schema={form.schema} />
        </div>
      </div>
    </div>
  );
}

// ─── Submissions View ─────────────────────────────────────────────────────────

function SubmissionsView({ auth }: { auth: AuthState }) {
  const allSubs = auth === "admin" ? submissions : userSubmissions;
  const [search, setSearch] = useState("");
  const [formFilter, setFormFilter] = useState("all");
  const [viewing, setViewing] = useState<Submission | null>(null);

  const filtered = useMemo(() => {
    let list = [...allSubs];
    if (formFilter !== "all") list = list.filter((s) => s.formId === formFilter);
    if (search.trim()) {
      const q = search.toLowerCase();
      list = list.filter(
        (s) =>
          s.formName.toLowerCase().includes(q) ||
          s.id.includes(q) ||
          s.userName.toLowerCase().includes(q),
      );
    }
    return list;
  }, [allSubs, search, formFilter]);

  if (viewing) {
    const form = allForms.find((f) => f.id === viewing.formId);
    return (
      <div className="flex-1 flex flex-col overflow-hidden min-h-0">
        <div className="flex items-center gap-2 px-5 py-2.5 border-b border-border text-xs bg-card/30 flex-shrink-0">
          <button className="text-muted-foreground hover:text-foreground transition-colors" onClick={() => setViewing(null)}>
            ← Submissions
          </button>
          <span className="text-border">/</span>
          <span className="text-foreground font-mono">#{viewing.id}</span>
          <span className={`ml-1 px-1.5 py-0.5 rounded border text-[10px] font-medium ${spaceColor(viewing.spaceId)}`}>
            {viewing.spaceName}
          </span>
        </div>
        <div className="flex-1 overflow-auto p-6">
          <div className="max-w-md">
            <div className="mb-5">
              <h2 className="text-sm font-semibold text-foreground">{viewing.formName}</h2>
              <p className="text-xs text-muted-foreground mt-1">
                {auth === "admin" ? `${viewing.userName} · ${viewing.userEmail} · ` : "Submitted "}
                {formatDate(viewing.createdAt)} · read-only
              </p>
            </div>
            {form && <FormRenderer schema={form.schema} readOnly initialValues={viewing.data} />}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 flex flex-col overflow-hidden min-h-0">
      <div className="flex items-center justify-between px-5 py-3 border-b border-border bg-card/30 flex-shrink-0 gap-3 flex-wrap">
        <div>
          <h1 className="text-sm font-semibold text-foreground">
            {auth === "admin" ? "All Submissions" : "My Submissions"}
          </h1>
          <p className="text-[11px] text-muted-foreground mt-0.5">{allSubs.length} total</p>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          <div className="relative">
            <span className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground">
              <IconSearch />
            </span>
            <input
              type="text"
              className="bg-secondary border border-border rounded pl-8 pr-3 py-1.5 text-xs text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-ring w-44"
              placeholder="Search…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          {auth === "admin" && (
            <select
              className="bg-secondary border border-border rounded px-2.5 py-1.5 text-xs text-foreground focus:outline-none focus:ring-1 focus:ring-ring"
              value={formFilter}
              onChange={(e) => setFormFilter(e.target.value)}
            >
              <option value="all">All Forms</option>
              {allForms.map((f) => <option key={f.id} value={f.id}>{f.name}</option>)}
            </select>
          )}
          <button className="px-2.5 py-1.5 text-[11px] font-medium text-muted-foreground border border-border rounded hover:border-primary/40 hover:text-foreground transition-colors">
            Export XLSX
          </button>
          <button className="px-2.5 py-1.5 text-[11px] font-medium text-muted-foreground border border-border rounded hover:border-primary/40 hover:text-foreground transition-colors">
            Export JSON
          </button>
        </div>
      </div>

      <div className="flex-1 overflow-auto">
        <table className="w-full text-xs border-collapse">
          <thead className="sticky top-0 bg-card z-10">
            <tr className="border-b border-border">
              <th className="text-left px-5 py-2.5 text-[10px] font-bold tracking-wider uppercase text-muted-foreground">#</th>
              <th className="text-left px-5 py-2.5 text-[10px] font-bold tracking-wider uppercase text-muted-foreground">Form</th>
              {auth === "admin" && (
                <th className="text-left px-5 py-2.5 text-[10px] font-bold tracking-wider uppercase text-muted-foreground">Submitted By</th>
              )}
              <th className="text-left px-5 py-2.5 text-[10px] font-bold tracking-wider uppercase text-muted-foreground">Space</th>
              <th className="text-left px-5 py-2.5 text-[10px] font-bold tracking-wider uppercase text-muted-foreground">Date</th>
              <th className="text-right px-5 py-2.5 text-[10px] font-bold tracking-wider uppercase text-muted-foreground"></th>
            </tr>
          </thead>
          <tbody>
            {filtered.length === 0 ? (
              <tr>
                <td colSpan={auth === "admin" ? 6 : 5} className="px-5 py-10 text-center text-muted-foreground text-xs">
                  No submissions match your filters
                </td>
              </tr>
            ) : (
              filtered.map((sub) => (
                <tr key={sub.id} className="border-b border-border/40 hover:bg-secondary/25 transition-colors group">
                  <td className="px-5 py-3 font-mono text-muted-foreground">{sub.id}</td>
                  <td className="px-5 py-3 text-foreground font-medium">{sub.formName}</td>
                  {auth === "admin" && (
                    <td className="px-5 py-3">
                      <div className="flex items-center gap-2">
                        <div className="w-5 h-5 rounded-full bg-secondary border border-border flex items-center justify-center text-[9px] font-bold text-muted-foreground flex-shrink-0">
                          {sub.userName.split(" ").map((n) => n[0]).join("")}
                        </div>
                        <div className="min-w-0">
                          <div className="text-foreground truncate">{sub.userName}</div>
                          <div className="text-[10px] text-muted-foreground truncate">{sub.userEmail}</div>
                        </div>
                      </div>
                    </td>
                  )}
                  <td className="px-5 py-3">
                    <span className={`inline-flex px-1.5 py-0.5 rounded border text-[10px] font-medium ${spaceColor(sub.spaceId)}`}>
                      {sub.spaceName}
                    </span>
                  </td>
                  <td className="px-5 py-3 font-mono text-muted-foreground">{formatDate(sub.createdAt)}</td>
                  <td className="px-5 py-3 text-right">
                    <button
                      className="text-primary text-[11px] opacity-0 group-hover:opacity-100 hover:text-primary/80 transition-all"
                      onClick={() => setViewing(sub)}
                    >
                      View →
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="px-5 py-2 border-t border-border bg-card/20 flex-shrink-0">
        <span className="text-[10px] text-muted-foreground font-mono">
          {filtered.length} of {allSubs.length} submissions
        </span>
      </div>
    </div>
  );
}

// ─── About View ───────────────────────────────────────────────────────────────

function AboutView() {
  const features = [
    { title: "JSON Schema Forms", desc: "Author forms using JSON Schema with full type support — strings, numbers, booleans, dates, enums, and conditional logic." },
    { title: "Section Builder", desc: "Visual drag-and-drop interface to arrange fields into multi-column sections without touching JSON." },
    { title: "Live Preview", desc: "See rendered form output update in real time as you edit the schema." },
    { title: "Role-based Access", desc: "Admins manage spaces and forms; users see only what they are permitted to access." },
    { title: "Submissions Tracking", desc: "Search, filter, and review every submission. Export as XLSX or JSON at any time." },
    { title: "Spaces", desc: "Organise forms into named spaces (e.g. HR, Finance, Operations) with per-space colour coding." },
  ];

  return (
    <div className="flex-1 overflow-auto p-6">
      <div className="max-w-2xl">
        <div className="flex items-center gap-3 mb-2">
          <LogoMark />
          <h1 className="text-base font-semibold text-foreground">FormCraft</h1>
          <span className="text-[10px] text-muted-foreground border border-border/60 rounded px-1.5 py-0.5 font-mono">v0.1.0</span>
        </div>
        <p className="text-xs text-muted-foreground mb-7 leading-relaxed">
          Dynamic form configuration and rendering platform for enterprise teams. Built for scale and designed to be operated without engineering involvement.
        </p>

        <div className="grid grid-cols-2 gap-3 mb-8">
          {features.map(({ title, desc }) => (
            <div key={title} className="bg-card border border-border rounded-lg p-4">
              <h3 className="text-xs font-semibold text-foreground mb-1.5">{title}</h3>
              <p className="text-[11px] text-muted-foreground leading-relaxed">{desc}</p>
            </div>
          ))}
        </div>

        <div className="border-t border-border/60 pt-5">
          <p className="text-[10px] text-muted-foreground">
            Built with React 19, Vite 8, Tailwind CSS v4, and JSON Schema Draft-07. Sign in with your Microsoft Entra work account.
          </p>
        </div>
      </div>
    </div>
  );
}

// ─── App Shell ────────────────────────────────────────────────────────────────

export default function App() {
  const { instance, accounts } = useMsal();
  const account = accounts[0] ?? null;

  const [nav, setNav] = useState<NavItem>("home");
  const [fillForm, setFillForm] = useState<Form | null>(null);
  const [profile, setProfile] = useState<AppUser | null>(null);
  const [profileError, setProfileError] = useState<string | null>(null);
  const [profileAttempt, setProfileAttempt] = useState(0);

  // Real identity + role come from the backend (`/api/me`), which provisions the
  // user on first login and reports whether they're an admin. We show a checking
  // screen until it resolves rather than guessing a role from the token.
  useEffect(() => {
    if (!account) {
      setProfile(null);
      setProfileError(null);
      return;
    }
    let cancelled = false;
    setProfileError(null);
    fetchMe(instance, account)
      .then((me) => {
        if (cancelled) return;
        setProfile({
          id: me.id,
          name: me.name,
          email: me.email,
          role: me.role,
          initials: initialsOf(me.name),
        });
      })
      .catch((err: Error) => {
        if (cancelled) return;
        setProfile(null);
        setProfileError(err.message || "Could not verify your session with the API.");
      });
    return () => {
      cancelled = true;
    };
  }, [instance, account?.localAccountId, profileAttempt]);

  const auth: AuthState = profile ? profile.role : "none";
  const user: AppUser | null = profile;

  const signIn = () => {
    if (!authConfigured) {
      setProfileError(
        "Entra ID is not configured — set VITE_ENTRA_CLIENT_ID and VITE_ENTRA_TENANT_ID in src/web/.env.",
      );
      return;
    }
    void instance.loginRedirect(apiRequest);
  };

  const signOut = () => {
    void instance.logoutRedirect({ postLogoutRedirectUri: window.location.origin });
    setProfile(null);
  };

  const retryProfile = () => setProfileAttempt((n) => n + 1);

  const handleOpenForm = (form: Form) => {
    setFillForm(form);
    setNav("my-forms");
  };

  // Nav items config
  const navItems: { id: NavItem; label: string; icon: React.ReactNode; adminOnly?: boolean; authRequired?: boolean }[] = [
    { id: "home", label: "Home", icon: <IconHome /> },
    { id: "my-forms", label: "My Forms", icon: <IconForms />, authRequired: true },
    { id: "submissions", label: "Submissions", icon: <IconSubmissions />, authRequired: true },
    { id: "management", label: "Management", icon: <IconManagement />, adminOnly: true, authRequired: true },
    { id: "about", label: "About", icon: <IconAbout /> },
  ];

  const visibleNavItems = navItems.filter((item) => {
    if (item.adminOnly && auth !== "admin") return false;
    return true;
  });

  // Ensure we don't stay on a hidden nav item after logout/role change
  const effectiveNav = visibleNavItems.some((n) => n.id === nav) ? nav : "home";

  // Signed into Entra but the role isn't resolved yet (or resolution failed) —
  // hold the app on a focused screen instead of flashing a guessed role.
  if (account && !profile) {
    return profileError ? (
      <SessionError message={profileError} onRetry={retryProfile} onSignOut={signOut} />
    ) : (
      <SessionChecking />
    );
  }

  return (
    <div className="flex h-full bg-background text-foreground font-sans overflow-hidden">
      {/* ── Left Sidebar ── */}
      <aside className="w-52 flex-shrink-0 bg-sidebar border-r border-border flex flex-col">
        {/* Logo */}
        <div className="flex items-center gap-2.5 px-4 py-3.5 border-b border-border">
          <LogoMark />
          <span className="text-sm font-bold tracking-tight text-foreground">FormCraft</span>
        </div>

        {/* Nav items */}
        <nav className="flex-1 flex flex-col gap-0.5 p-2 pt-3">
          {visibleNavItems.map((item) => {
            const isActive = effectiveNav === item.id;
            const isAuthRequired = item.authRequired && auth === "none";
            return (
              <button
                key={item.id}
                disabled={isAuthRequired}
                className={`w-full flex items-center gap-2.5 px-3 py-2 rounded text-xs font-medium transition-colors text-left ${
                  isAuthRequired
                    ? "text-muted-foreground/30 cursor-not-allowed"
                    : isActive
                    ? "bg-primary/10 text-primary"
                    : "text-muted-foreground hover:text-foreground hover:bg-secondary/40"
                }`}
                onClick={() => {
                  if (!isAuthRequired) {
                    setNav(item.id);
                    setFillForm(null);
                  }
                }}
              >
                <span className={`flex-shrink-0 ${isActive ? "text-primary" : ""}`}>{item.icon}</span>
                <span>{item.label}</span>
                {item.adminOnly && auth === "admin" && (
                  <span className="ml-auto text-[9px] font-mono text-primary/60 border border-primary/20 rounded px-1 py-px">
                    admin
                  </span>
                )}
              </button>
            );
          })}
        </nav>

        {/* User / Login footer */}
        <div className="border-t border-border p-3">
          {auth === "none" ? (
            <button
              className="w-full flex items-center justify-center gap-2 px-3 py-2 bg-primary text-primary-foreground text-xs font-semibold rounded hover:bg-primary/90 active:scale-[0.98] transition-all"
              onClick={signIn}
            >
              <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9" />
              </svg>
              Sign In
            </button>
          ) : (
            <div className="flex items-center gap-2.5">
              <div
                className={`w-7 h-7 rounded-full flex items-center justify-center text-[10px] font-bold flex-shrink-0 ${
                  auth === "admin"
                    ? "bg-primary/20 text-primary border border-primary/30"
                    : "bg-accent/15 text-accent border border-accent/30"
                }`}
              >
                {user!.initials}
              </div>
              <div className="flex-1 min-w-0">
                <div className="text-[11px] font-semibold text-foreground truncate">{user!.name}</div>
                <div className="text-[10px] text-muted-foreground capitalize">{user!.role}</div>
              </div>
              <button
                className="w-6 h-6 flex items-center justify-center rounded text-muted-foreground hover:text-foreground hover:bg-secondary/60 transition-colors flex-shrink-0"
                title="Sign out"
                onClick={signOut}
              >
                <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l-3 3m0 0l3 3m-3-3h12.75" />
                </svg>
              </button>
            </div>
          )}
        </div>
      </aside>

      {/* ── Main content ── */}
      <div className="flex-1 flex flex-col overflow-hidden min-w-0">
        {effectiveNav === "home" && (
          <HomeView
            auth={auth}
            user={user}
            onOpenForm={handleOpenForm}
            onLogin={signIn}
          />
        )}

        {effectiveNav === "my-forms" && auth !== "none" && (
          fillForm ? (
            <FillFormView form={fillForm} onBack={() => setFillForm(null)} />
          ) : (
            <MyFormsView auth={auth} onOpenForm={(f) => setFillForm(f)} />
          )
        )}

        {effectiveNav === "submissions" && auth !== "none" && (
          <SubmissionsView auth={auth} />
        )}

        {effectiveNav === "management" && auth === "admin" && (
          <AdminPortal />
        )}

        {effectiveNav === "about" && <AboutView />}
      </div>

    </div>
  );
}
