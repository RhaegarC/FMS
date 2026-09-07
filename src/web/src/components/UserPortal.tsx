import { useState } from "react";
import { userAccessibleForms, userSubmissions, spaces, forms, normalUser } from "@/data/mockData";
import type { Form, Submission } from "@/data/mockData";
import FormRenderer from "./FormRenderer";
import { spaceColor, formatDate } from "@/utils/helpers";

type UserView = "forms" | "fill" | "submissions" | "detail";

function IconSearch() {
  return (
    <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
    </svg>
  );
}

export default function UserPortal() {
  const [view, setView] = useState<UserView>("forms");
  const [selectedForm, setSelectedForm] = useState<Form | null>(null);
  const [selectedSubmission, setSelectedSubmission] = useState<Submission | null>(null);
  const [search, setSearch] = useState("");

  const filteredSubmissions = userSubmissions.filter((s) => {
    if (!search.trim()) return true;
    const q = search.toLowerCase();
    return s.formName.toLowerCase().includes(q) || s.id.includes(q);
  });

  const navItem = (v: UserView, label: string, count?: number) => (
    <button
      className={`w-full flex items-center justify-between px-3 py-2 rounded text-xs font-medium transition-colors ${
        view === v || (view === "fill" && v === "forms") || (view === "detail" && v === "submissions")
          ? "bg-primary/10 text-primary"
          : "text-muted-foreground hover:text-foreground hover:bg-secondary/40"
      }`}
      onClick={() => setView(v)}
    >
      <span>{label}</span>
      {count !== undefined && (
        <span className="text-[10px] font-mono text-muted-foreground">{count}</span>
      )}
    </button>
  );

  return (
    <div className="flex h-full overflow-hidden">
      {/* Sidebar */}
      <aside className="w-48 flex-shrink-0 bg-sidebar border-r border-border flex flex-col">
        <div className="p-4 border-b border-border">
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-full bg-accent/15 border border-accent/30 text-accent text-xs font-bold flex items-center justify-center flex-shrink-0">
              {normalUser.initials}
            </div>
            <div className="min-w-0">
              <div className="text-xs font-semibold text-foreground truncate">{normalUser.name}</div>
              <div className="text-[10px] text-muted-foreground truncate">{normalUser.email}</div>
            </div>
          </div>
        </div>
        <nav className="p-2 flex flex-col gap-0.5">
          {navItem("forms", "My Forms", userAccessibleForms.length)}
          {navItem("submissions", "Submissions", userSubmissions.length)}
        </nav>
        <div className="mt-auto p-3 border-t border-border">
          <div className="flex items-center gap-1.5">
            <div className="w-1.5 h-1.5 rounded-full bg-accent" />
            <span className="text-[10px] text-muted-foreground">Entra ID · user</span>
          </div>
        </div>
      </aside>

      {/* Main */}
      <div className="flex-1 flex flex-col overflow-hidden min-w-0">
        {view === "forms" && (
          <div className="flex-1 overflow-auto p-6">
            <div className="mb-6">
              <h1 className="text-base font-semibold text-foreground">My Forms</h1>
              <p className="text-xs text-muted-foreground mt-0.5">Forms you are authorised to fill and submit.</p>
            </div>
            <div className="grid gap-4" style={{ gridTemplateColumns: "repeat(auto-fill, minmax(260px, 1fr))" }}>
              {userAccessibleForms.map((form) => {
                const space = spaces.find((s) => s.id === form.spaceId);
                const myCount = userSubmissions.filter((s) => s.formId === form.id).length;
                const fieldCount = Object.keys(form.schema.properties).length;
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
                        <div className="text-xl font-bold text-foreground font-mono leading-none">{myCount}</div>
                        <div className="text-[10px] text-muted-foreground mt-0.5">my sub.</div>
                      </div>
                    </div>
                    <div className="flex items-center justify-between">
                      <div className="flex gap-3">
                        <span className="text-[11px] text-muted-foreground">{fieldCount} fields</span>
                        {form.schema.if && (
                          <span className="text-[11px] text-accent">if/then</span>
                        )}
                      </div>
                      <span className="text-[10px] text-muted-foreground font-mono">upd. {form.updatedAt}</span>
                    </div>
                    <button
                      className="mt-auto w-full py-1.5 bg-primary/10 hover:bg-primary/20 text-primary text-xs font-semibold rounded transition-colors"
                      onClick={() => { setSelectedForm(form); setView("fill"); }}
                    >
                      Open Form →
                    </button>
                  </div>
                );
              })}
            </div>
          </div>
        )}

        {view === "fill" && selectedForm && (
          <div className="flex-1 overflow-auto flex flex-col">
            <div className="flex items-center gap-2 px-5 py-2.5 border-b border-border text-xs bg-card/30 flex-shrink-0">
              <button
                className="text-muted-foreground hover:text-foreground transition-colors"
                onClick={() => setView("forms")}
              >
                ← My Forms
              </button>
              <span className="text-border">/</span>
              <span className="text-foreground font-medium">{selectedForm.name}</span>
            </div>
            <div className="flex-1 overflow-auto p-6">
              <div className="max-w-md">
                <h2 className="text-sm font-semibold text-foreground mb-1">
                  {selectedForm.schema.title ?? selectedForm.name}
                </h2>
                {selectedForm.schema.description && (
                  <p className="text-xs text-muted-foreground mb-5 leading-relaxed">{selectedForm.schema.description}</p>
                )}
                <FormRenderer
                  schema={selectedForm.schema}
                  onSubmit={() => {
                    /* In a real app, POST to API */
                  }}
                />
              </div>
            </div>
          </div>
        )}

        {view === "submissions" && (
          <div className="flex-1 flex flex-col overflow-hidden min-h-0">
            <div className="flex items-center justify-between px-5 py-3 border-b border-border bg-card/30 flex-shrink-0 gap-3 flex-wrap">
              <div>
                <h1 className="text-sm font-semibold text-foreground">My Submissions</h1>
                <p className="text-[11px] text-muted-foreground mt-0.5">{userSubmissions.length} total</p>
              </div>
              <div className="flex items-center gap-2">
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
                    <th className="text-left px-5 py-2.5 text-[10px] font-bold tracking-wider uppercase text-muted-foreground">Space</th>
                    <th className="text-left px-5 py-2.5 text-[10px] font-bold tracking-wider uppercase text-muted-foreground">Submitted</th>
                    <th className="text-right px-5 py-2.5 text-[10px] font-bold tracking-wider uppercase text-muted-foreground"></th>
                  </tr>
                </thead>
                <tbody>
                  {filteredSubmissions.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="px-5 py-10 text-center text-muted-foreground">
                        No submissions yet
                      </td>
                    </tr>
                  ) : (
                    filteredSubmissions.map((sub) => (
                      <tr key={sub.id} className="border-b border-border/40 hover:bg-secondary/25 transition-colors group">
                        <td className="px-5 py-3 font-mono text-muted-foreground">{sub.id}</td>
                        <td className="px-5 py-3 text-foreground font-medium">{sub.formName}</td>
                        <td className="px-5 py-3">
                          <span className={`inline-flex px-1.5 py-0.5 rounded border text-[10px] font-medium ${spaceColor(sub.spaceId)}`}>
                            {sub.spaceName}
                          </span>
                        </td>
                        <td className="px-5 py-3 font-mono text-muted-foreground">{formatDate(sub.createdAt)}</td>
                        <td className="px-5 py-3 text-right">
                          <button
                            className="text-primary text-[11px] opacity-0 group-hover:opacity-100 hover:text-primary/80 transition-all"
                            onClick={() => { setSelectedSubmission(sub); setView("detail"); }}
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
          </div>
        )}

        {view === "detail" && selectedSubmission && (
          <div className="flex-1 overflow-auto flex flex-col">
            <div className="flex items-center gap-2 px-5 py-2.5 border-b border-border text-xs bg-card/30 flex-shrink-0">
              <button
                className="text-muted-foreground hover:text-foreground transition-colors"
                onClick={() => setView("submissions")}
              >
                ← Submissions
              </button>
              <span className="text-border">/</span>
              <span className="text-foreground font-mono">#{selectedSubmission.id}</span>
              <span className={`ml-1 px-1.5 py-0.5 rounded border text-[10px] font-medium ${spaceColor(selectedSubmission.spaceId)}`}>
                {selectedSubmission.spaceName}
              </span>
            </div>
            <div className="flex-1 overflow-auto p-6">
              <div className="max-w-md">
                <div className="mb-5">
                  <h2 className="text-sm font-semibold text-foreground">{selectedSubmission.formName}</h2>
                  <p className="text-xs text-muted-foreground mt-1">
                    Submitted {formatDate(selectedSubmission.createdAt)} · read-only
                  </p>
                </div>
                {(() => {
                  const form = forms.find((f) => f.id === selectedSubmission.formId);
                  return form ? (
                    <FormRenderer schema={form.schema} readOnly initialValues={selectedSubmission.data} />
                  ) : null;
                })()}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
