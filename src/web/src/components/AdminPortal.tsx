import { useState, useMemo, useRef, useEffect } from "react";
import { spaces as initialSpaces, forms as initialForms, submissions } from "@/data/mockData";
import type { Form, Space, Submission, JsonSchema } from "@/data/mockData";
import FormRenderer from "./FormRenderer";
import SectionBuilder from "./SectionBuilder";
import { spaceColor, formatDate } from "@/utils/helpers";

type AdminTab = "editor" | "submissions";
type EditorPane = "json" | "sections";

const SPACE_COLORS = [
  "#8b5cf6", "#3d7fff", "#f59e0b", "#10b981",
  "#f43f5e", "#06b6d4", "#f97316", "#6366f1",
];

const DEFAULT_SCHEMA = {
  title: "Untitled Form",
  description: "",
  type: "object",
  properties: {
    fullName: { type: "string", title: "Full Name" },
  },
  required: ["fullName"],
};

let nextId = 100;
const uid = () => `${++nextId}`;

function IconSearch() {
  return (
    <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
    </svg>
  );
}

function IconDownload() {
  return (
    <svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
    </svg>
  );
}

function IconPencil() {
  return (
    <svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
    </svg>
  );
}

function IconTrash() {
  return (
    <svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
    </svg>
  );
}

function IconPlus() {
  return (
    <svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4" />
    </svg>
  );
}

function InlineInput({
  value,
  onCommit,
  onCancel,
  className = "",
}: {
  value: string;
  onCommit: (v: string) => void;
  onCancel: () => void;
  className?: string;
}) {
  const [draft, setDraft] = useState(value);
  const ref = useRef<HTMLInputElement>(null);
  useEffect(() => { ref.current?.focus(); ref.current?.select(); }, []);

  return (
    <input
      ref={ref}
      className={`bg-secondary border border-primary/60 rounded px-1.5 py-0.5 text-xs text-foreground focus:outline-none focus:ring-1 focus:ring-ring ${className}`}
      value={draft}
      onChange={(e) => setDraft(e.target.value)}
      onKeyDown={(e) => {
        if (e.key === "Enter") { e.preventDefault(); if (draft.trim()) onCommit(draft.trim()); }
        if (e.key === "Escape") onCancel();
      }}
      onBlur={() => { if (draft.trim()) onCommit(draft.trim()); else onCancel(); }}
    />
  );
}

// ─── Unified creation modal ───────────────────────────────────────

function UnifiedCreateModal({
  spaces,
  onClose,
  onCreateSpace,
  onCreateForm,
}: {
  spaces: Space[];
  onClose: () => void;
  onCreateSpace: (name: string, description: string) => void;
  onCreateForm: (spaceId: string, name: string, description: string) => void;
}) {
  const [type, setType] = useState<"Space" | "Form">("Space");
  const [spaceId, setSpaceId] = useState(spaces[0]?.id ?? "");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [nameError, setNameError] = useState("");
  const nameRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    nameRef.current?.focus();
    const handler = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [onClose]);

  // Reset space-specific error when type changes
  const handleTypeChange = (t: "Space" | "Form") => {
    setType(t);
    setNameError("");
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) { setNameError("Name is required"); return; }
    if (type === "Space") {
      onCreateSpace(name.trim(), description.trim());
    } else {
      onCreateForm(spaceId, name.trim(), description.trim());
    }
  };

  const inputBase =
    "w-full bg-background border rounded px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-ring transition-colors";

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/60 backdrop-blur-[2px]" onClick={onClose} />

      <div className="relative bg-card border border-border rounded-lg shadow-2xl w-full max-w-sm mx-4 overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-4 border-b border-border">
          <h2 className="text-sm font-semibold text-foreground">
            {type === "Space" ? "New Space" : "New Form"}
          </h2>
          <button
            className="w-6 h-6 flex items-center justify-center rounded text-muted-foreground hover:text-foreground hover:bg-secondary/60 transition-colors"
            onClick={onClose}
          >
            <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Body */}
        <form onSubmit={handleSubmit} className="px-5 py-5 flex flex-col gap-4">
          {/* Type */}
          <div className="flex flex-col gap-1.5">
            <label className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
              Type<span className="text-danger ml-0.5">*</span>
            </label>
            <select
              className={`${inputBase} border-border`}
              value={type}
              onChange={(e) => handleTypeChange(e.target.value as "Space" | "Form")}
            >
              <option value="Space">Space</option>
              <option value="Form">Form</option>
            </select>
          </div>

          {/* Space (only for Form type) */}
          <div className="flex flex-col gap-1.5">
            <label className={`text-[11px] font-semibold uppercase tracking-wide transition-colors ${type === "Space" ? "text-muted-foreground/40" : "text-muted-foreground"}`}>
              Space<span className="text-danger ml-0.5">*</span>
            </label>
            <select
              className={`${inputBase} transition-opacity ${type === "Space" ? "border-border/40 opacity-40 cursor-not-allowed" : "border-border"}`}
              value={spaceId}
              onChange={(e) => setSpaceId(e.target.value)}
              disabled={type === "Space"}
            >
              {spaces.length === 0 && <option value="">No spaces yet</option>}
              {spaces.map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
          </div>

          {/* Name */}
          <div className="flex flex-col gap-1.5">
            <label className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
              Name<span className="text-danger ml-0.5">*</span>
            </label>
            <input
              ref={nameRef}
              type="text"
              className={`${inputBase} ${nameError ? "border-danger/60" : "border-border"}`}
              placeholder={type === "Space" ? "e.g. Human Resources" : "e.g. Expense Report"}
              value={name}
              onChange={(e) => { setName(e.target.value); if (nameError) setNameError(""); }}
            />
            {nameError && <p className="text-[11px] text-danger">{nameError}</p>}
          </div>

          {/* Description */}
          <div className="flex flex-col gap-1.5">
            <label className="text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
              Description
            </label>
            <textarea
              className={`${inputBase} border-border min-h-[68px] resize-none leading-relaxed`}
              placeholder={type === "Space" ? "Brief description of this space…" : "What is this form for?…"}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={3}
            />
          </div>

          {/* Footer */}
          <div className="flex items-center justify-end gap-2 pt-1">
            <button
              type="button"
              className="px-3.5 py-1.5 text-xs font-medium text-muted-foreground hover:text-foreground border border-border rounded hover:border-primary/40 transition-colors"
              onClick={onClose}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="px-4 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded hover:bg-primary/90 active:scale-[0.98] transition-all"
            >
              Create
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default function AdminPortal() {
  const [spaces, setSpaces] = useState<Space[]>(initialSpaces);
  const [forms, setForms] = useState<Form[]>(initialForms);

  const [selectedFormId, setSelectedFormId] = useState<string>(forms[0].id);
  const [activeTab, setActiveTab] = useState<AdminTab>("editor");
  const [editorPane, setEditorPane] = useState<EditorPane>("json");
  const [schemaText, setSchemaText] = useState(() => JSON.stringify(forms[0].schema, null, 2));
  const [schemaChanged, setSchemaChanged] = useState(false);
  const [expandedSpaces, setExpandedSpaces] = useState<Set<string>>(new Set(spaces.map((s) => s.id)));

  // Sidebar editing state
  const [editingSpaceId, setEditingSpaceId] = useState<string | null>(null);
  const [editingFormId, setEditingFormId] = useState<string | null>(null);
  const [confirmDeleteSpace, setConfirmDeleteSpace] = useState<string | null>(null);
  const [confirmDeleteForm, setConfirmDeleteForm] = useState<string | null>(null);
  const [colorPickerSpaceId, setColorPickerSpaceId] = useState<string | null>(null);

  // Creation modal
  const [createModalOpen, setCreateModalOpen] = useState(false);

  // Submissions
  const [submissionSearch, setSubmissionSearch] = useState("");
  const [submissionFormFilter, setSubmissionFormFilter] = useState("all");
  const [viewingSubmission, setViewingSubmission] = useState<Submission | null>(null);

  const selectedForm = forms.find((f) => f.id === selectedFormId) ?? forms[0];
  const selectedSpace = spaces.find((s) => s.id === selectedForm?.spaceId);

  const parsedSchema = useMemo(() => {
    try { return JSON.parse(schemaText); } catch { return null; }
  }, [schemaText]);

  // ─── Space actions ───────────────────────────────────────────────

  const createSpace = (name: string, description: string) => {
    const id = `space-${uid()}`;
    const color = SPACE_COLORS[spaces.length % SPACE_COLORS.length];
    setSpaces((prev) => [...prev, { id, name, description, color }]);
    setExpandedSpaces((prev) => new Set([...prev, id]));
    setCreateModalOpen(false);
  };

  const renameSpace = (id: string, name: string) => {
    setSpaces((prev) => prev.map((s) => (s.id === id ? { ...s, name } : s)));
    setEditingSpaceId(null);
  };

  const setSpaceColor = (id: string, color: string) => {
    setSpaces((prev) => prev.map((s) => (s.id === id ? { ...s, color } : s)));
    setColorPickerSpaceId(null);
  };

  const deleteSpace = (id: string) => {
    const spaceFormIds = forms.filter((f) => f.spaceId === id).map((f) => f.id);
    setForms((prev) => prev.filter((f) => f.spaceId !== id));
    setSpaces((prev) => prev.filter((s) => s.id !== id));
    setConfirmDeleteSpace(null);
    if (spaceFormIds.includes(selectedFormId)) {
      const remaining = forms.filter((f) => f.spaceId !== id);
      if (remaining.length > 0) loadForm(remaining[0]);
    }
  };

  // ─── Form actions ────────────────────────────────────────────────

  const createForm = (spaceId: string, name: string, description: string) => {
    const id = `form-${uid()}`;
    const newForm: Form = {
      id,
      spaceId,
      name,
      schema: { ...DEFAULT_SCHEMA, title: name, description },
      updatedAt: new Date().toISOString().slice(0, 10),
      submissionCount: 0,
    };
    setForms((prev) => [...prev, newForm]);
    setExpandedSpaces((prev) => new Set([...prev, spaceId]));
    setCreateModalOpen(false);
    loadForm(newForm);
  };

  const renameForm = (id: string, name: string) => {
    setForms((prev) =>
      prev.map((f) =>
        f.id === id
          ? { ...f, name, schema: { ...f.schema, title: name } }
          : f,
      ),
    );
    if (id === selectedFormId) {
      setSchemaText((prev) => {
        try {
          const parsed = JSON.parse(prev);
          parsed.title = name;
          return JSON.stringify(parsed, null, 2);
        } catch { return prev; }
      });
    }
    setEditingFormId(null);
  };

  const deleteForm = (id: string) => {
    setForms((prev) => prev.filter((f) => f.id !== id));
    setConfirmDeleteForm(null);
    if (id === selectedFormId) {
      const remaining = forms.filter((f) => f.id !== id);
      if (remaining.length > 0) loadForm(remaining[0]);
    }
  };

  const loadForm = (form: Form) => {
    setSelectedFormId(form.id);
    setSchemaText(JSON.stringify(form.schema, null, 2));
    setSchemaChanged(false);
    setActiveTab("editor");
  };

  const saveSchema = () => {
    if (!parsedSchema) return;
    setForms((prev) =>
      prev.map((f) =>
        f.id === selectedFormId
          ? { ...f, schema: parsedSchema, updatedAt: new Date().toISOString().slice(0, 10) }
          : f,
      ),
    );
    setSchemaChanged(false);
  };

  const handleSchemaChange = (text: string) => {
    setSchemaText(text);
    setSchemaChanged(true);
  };

  const handleSectionBuilderChange = (updated: JsonSchema) => {
    setSchemaText(JSON.stringify(updated, null, 2));
    setSchemaChanged(true);
  };

  const handlePrettify = () => {
    try { const pretty = JSON.stringify(JSON.parse(schemaText), null, 2); setSchemaText(pretty); } catch { /* ignore */ }
  };

  const toggleSpace = (id: string) => {
    setExpandedSpaces((prev) => { const n = new Set(prev); n.has(id) ? n.delete(id) : n.add(id); return n; });
  };

  // ─── Submissions ─────────────────────────────────────────────────

  const filteredSubmissions = useMemo(() => {
    let list = [...submissions];
    if (submissionFormFilter !== "all") list = list.filter((s) => s.formId === submissionFormFilter);
    if (submissionSearch.trim()) {
      const q = submissionSearch.toLowerCase();
      list = list.filter(
        (s) =>
          s.userName.toLowerCase().includes(q) ||
          s.formName.toLowerCase().includes(q) ||
          s.id.includes(q) ||
          s.userEmail.toLowerCase().includes(q),
      );
    }
    return list;
  }, [submissionSearch, submissionFormFilter]);

  // ─── Submission detail view ───────────────────────────────────────

  if (viewingSubmission) {
    const form = forms.find((f) => f.id === viewingSubmission.formId);
    return (
      <div className="flex flex-col h-full">
        <div className="flex items-center gap-2 px-5 py-2.5 border-b border-border text-xs bg-card/40 flex-shrink-0">
          <button className="text-muted-foreground hover:text-foreground transition-colors" onClick={() => setViewingSubmission(null)}>
            ← Submissions
          </button>
          <span className="text-border">/</span>
          <span className="text-foreground font-mono">#{viewingSubmission.id}</span>
          <span className={`ml-2 px-1.5 py-0.5 rounded border text-[10px] font-medium ${spaceColor(viewingSubmission.spaceId)}`}>
            {viewingSubmission.spaceName}
          </span>
        </div>
        <div className="flex-1 overflow-auto p-6">
          <div className="max-w-md">
            <div className="mb-5">
              <h2 className="text-sm font-semibold text-foreground">{viewingSubmission.formName}</h2>
              <p className="text-xs text-muted-foreground mt-1">
                {viewingSubmission.userName} · {viewingSubmission.userEmail} · {formatDate(viewingSubmission.createdAt)}
              </p>
            </div>
            {form && <FormRenderer schema={form.schema} readOnly initialValues={viewingSubmission.data} />}
          </div>
        </div>
      </div>
    );
  }

  // ─── Main layout ─────────────────────────────────────────────────

  return (
    <>
    <div className="flex h-full overflow-hidden">
      {/* ── Sidebar ── */}
      <aside className="w-56 flex-shrink-0 bg-sidebar border-r border-border flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-3 py-2.5 border-b border-border">
          <span className="text-[10px] font-bold tracking-widest uppercase text-muted-foreground">Spaces</span>
          <button
            className="w-5 h-5 flex items-center justify-center rounded text-muted-foreground hover:text-foreground hover:bg-secondary/60 transition-colors"
            title="New space or form"
            onClick={() => setCreateModalOpen(true)}
          >
            <IconPlus />
          </button>
        </div>

        {/* Space tree */}
        <nav className="flex-1 overflow-y-auto py-1">
          {spaces.map((space) => {
            const spaceForms = forms.filter((f) => f.spaceId === space.id);
            const expanded = expandedSpaces.has(space.id);
            const isConfirmingDelete = confirmDeleteSpace === space.id;
            const isPickingColor = colorPickerSpaceId === space.id;

            return (
              <div key={space.id}>
                {/* Space row */}
                <div className="group relative flex items-center gap-1.5 px-2 py-1 hover:bg-secondary/30 transition-colors">
                  {/* Chevron + toggle — standalone button */}
                  <button
                    className="flex-shrink-0 flex items-center justify-center w-4"
                    onClick={() => toggleSpace(space.id)}
                  >
                    <svg
                      className={`w-2.5 h-2.5 text-muted-foreground transition-transform ${expanded ? "" : "-rotate-90"}`}
                      fill="currentColor" viewBox="0 0 20 20"
                    >
                      <path fillRule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clipRule="evenodd" />
                    </svg>
                  </button>
                  {/* Color swatch — separate button, not nested */}
                  <button
                    className="w-2.5 h-2.5 rounded-full flex-shrink-0 ring-1 ring-white/10 hover:ring-white/30 transition-all"
                    style={{ backgroundColor: space.color }}
                    title="Change color"
                    onClick={() => setColorPickerSpaceId(isPickingColor ? null : space.id)}
                  />
                  {/* Name / inline rename */}
                  {editingSpaceId === space.id ? (
                    <InlineInput
                      value={space.name}
                      onCommit={(v) => renameSpace(space.id, v)}
                      onCancel={() => setEditingSpaceId(null)}
                      className="flex-1 min-w-0"
                    />
                  ) : (
                    <button
                      className="flex-1 min-w-0 text-left"
                      onClick={() => toggleSpace(space.id)}
                    >
                      <span className="text-xs text-secondary-foreground font-medium truncate block">{space.name}</span>
                    </button>
                  )}

                  {/* Space actions */}
                  {editingSpaceId !== space.id && (
                    <div className="flex items-center gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0">
                      <button
                        className="w-5 h-5 flex items-center justify-center rounded text-muted-foreground hover:text-foreground hover:bg-secondary/60 transition-colors"
                        title="Rename space"
                        onClick={(e) => { e.stopPropagation(); setEditingSpaceId(space.id); }}
                      >
                        <IconPencil />
                      </button>
                      <button
                        className="w-5 h-5 flex items-center justify-center rounded text-muted-foreground hover:text-danger hover:bg-danger/10 transition-colors"
                        title="Delete space"
                        onClick={(e) => { e.stopPropagation(); setConfirmDeleteSpace(space.id); }}
                      >
                        <IconTrash />
                      </button>
                    </div>
                  )}
                </div>

                {/* Color picker */}
                {isPickingColor && (
                  <div className="flex flex-wrap gap-1.5 px-6 py-2 bg-secondary/30 border-b border-border/50">
                    {SPACE_COLORS.map((c) => (
                      <button
                        key={c}
                        className={`w-4 h-4 rounded-full ring-1 transition-all ${space.color === c ? "ring-white/80 scale-110" : "ring-white/10 hover:ring-white/40"}`}
                        style={{ backgroundColor: c }}
                        onClick={() => setSpaceColor(space.id, c)}
                      />
                    ))}
                  </div>
                )}

                {/* Delete space confirm */}
                {isConfirmingDelete && (
                  <div className="mx-2 mb-1 px-3 py-2 bg-danger/10 border border-danger/30 rounded text-[11px]">
                    <p className="text-foreground mb-1.5">Delete <span className="font-semibold">{space.name}</span> and its {spaceForms.length} form{spaceForms.length !== 1 ? "s" : ""}?</p>
                    <div className="flex gap-1.5">
                      <button className="px-2 py-0.5 bg-danger text-white rounded text-[10px] font-medium" onClick={() => deleteSpace(space.id)}>Delete</button>
                      <button className="px-2 py-0.5 text-muted-foreground hover:text-foreground text-[10px]" onClick={() => setConfirmDeleteSpace(null)}>Cancel</button>
                    </div>
                  </div>
                )}

                {/* Forms under space */}
                {expanded && spaceForms.map((form) => {
                  const isConfirmingFormDelete = confirmDeleteForm === form.id;
                  return (
                    <div key={form.id}>
                      <div
                        className={`group flex items-center gap-1.5 pl-7 pr-2 py-1 transition-colors cursor-pointer ${
                          form.id === selectedFormId ? "bg-primary/10" : "hover:bg-secondary/25"
                        }`}
                        onClick={() => { if (editingFormId !== form.id) loadForm(form); }}
                      >
                        <svg className={`w-3 h-3 flex-shrink-0 ${form.id === selectedFormId ? "text-primary/60" : "text-muted-foreground/60"}`} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                        </svg>

                        {editingFormId === form.id ? (
                          <InlineInput
                            value={form.name}
                            onCommit={(v) => renameForm(form.id, v)}
                            onCancel={() => setEditingFormId(null)}
                            className="flex-1 min-w-0"
                          />
                        ) : (
                          <span className={`text-[11px] truncate flex-1 ${form.id === selectedFormId ? "text-primary font-medium" : "text-muted-foreground"}`}>
                            {form.name}
                          </span>
                        )}

                        {editingFormId !== form.id && (
                          <div className="flex items-center gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0">
                            <button
                              className="w-4 h-4 flex items-center justify-center rounded text-muted-foreground hover:text-foreground hover:bg-secondary/60 transition-colors"
                              title="Rename"
                              onClick={(e) => { e.stopPropagation(); loadForm(form); setEditingFormId(form.id); }}
                            >
                              <IconPencil />
                            </button>
                            <button
                              className="w-4 h-4 flex items-center justify-center rounded text-muted-foreground hover:text-danger hover:bg-danger/10 transition-colors"
                              title="Delete"
                              onClick={(e) => { e.stopPropagation(); setConfirmDeleteForm(form.id); }}
                            >
                              <IconTrash />
                            </button>
                          </div>
                        )}
                      </div>

                      {/* Delete form confirm */}
                      {isConfirmingFormDelete && (
                        <div className="mx-2 mb-1 px-3 py-2 bg-danger/10 border border-danger/30 rounded text-[11px]">
                          <p className="text-foreground mb-1.5">Delete <span className="font-semibold">{form.name}</span>?</p>
                          <div className="flex gap-1.5">
                            <button className="px-2 py-0.5 bg-danger text-white rounded text-[10px] font-medium" onClick={() => deleteForm(form.id)}>Delete</button>
                            <button className="px-2 py-0.5 text-muted-foreground hover:text-foreground text-[10px]" onClick={() => setConfirmDeleteForm(null)}>Cancel</button>
                          </div>
                        </div>
                      )}
                    </div>
                  );
                })}

              </div>
            );
          })}
        </nav>
      </aside>

      {/* ── Main content ── */}
      <div className="flex-1 flex flex-col overflow-hidden min-w-0">
        {/* Breadcrumb / tab bar */}
        <div className="flex items-center justify-between px-4 py-2 border-b border-border bg-card/30 flex-shrink-0">
          <div className="flex items-center gap-2 text-xs min-w-0">
            <span className="text-muted-foreground truncate">{selectedSpace?.name ?? "—"}</span>
            <span className="text-border flex-shrink-0">/</span>
            <span className="text-foreground font-medium truncate">{selectedForm?.name ?? "—"}</span>
            <span className="flex-shrink-0 text-[10px] text-muted-foreground border border-border/60 rounded px-1.5 py-0.5 font-mono">
              {selectedForm?.submissionCount ?? 0} sub
            </span>
          </div>
          <div className="flex gap-0.5 flex-shrink-0 ml-3">
            {(["editor", "submissions"] as AdminTab[]).map((tab) => (
              <button
                key={tab}
                className={`px-3 py-1 text-[11px] font-medium rounded transition-colors ${
                  activeTab === tab ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:text-foreground"
                }`}
                onClick={() => setActiveTab(tab)}
              >
                {tab === "editor" ? "Form Editor" : "Submissions"}
              </button>
            ))}
          </div>
        </div>

        {/* ── Form Editor ── */}
        {activeTab === "editor" && (
          <div className="flex-1 flex overflow-hidden min-h-0">
            {/* Left pane: JSON or Sections */}
            <div className="flex-1 flex flex-col border-r border-border overflow-hidden min-w-0">
              {/* Pane header */}
              <div className="flex items-center justify-between px-3 py-2 border-b border-border bg-card/20 flex-shrink-0 gap-2">
                {/* JSON / Sections toggle */}
                <div className="flex items-center bg-secondary border border-border rounded p-0.5 gap-px">
                  {(["json", "sections"] as EditorPane[]).map((pane) => (
                    <button
                      key={pane}
                      className={`px-2.5 py-0.5 text-[11px] font-semibold rounded capitalize transition-all ${
                        editorPane === pane
                          ? "bg-primary/20 text-primary"
                          : "text-muted-foreground hover:text-foreground"
                      }`}
                      onClick={() => setEditorPane(pane)}
                    >
                      {pane === "json" ? "JSON" : "Sections"}
                    </button>
                  ))}
                </div>

                {editorPane === "json" && (
                  <div className="flex items-center gap-1.5">
                    <span className={`w-1.5 h-1.5 rounded-full flex-shrink-0 ${parsedSchema ? "bg-success" : "bg-danger"}`} />
                    <span className={`text-[10px] font-mono ${parsedSchema ? "text-success" : "text-danger"}`}>
                      {parsedSchema ? "valid" : "parse error"}
                    </span>
                    <button
                      className="text-[10px] text-muted-foreground hover:text-foreground transition-colors px-2 py-0.5 border border-border/60 rounded hover:border-primary/40 ml-1"
                      onClick={handlePrettify}
                    >
                      Prettify
                    </button>
                  </div>
                )}

                {editorPane === "sections" && !parsedSchema && (
                  <span className="text-[10px] text-danger font-mono">Fix JSON first</span>
                )}

                <button
                  disabled={!schemaChanged || !parsedSchema}
                  className={`text-[10px] px-2.5 py-0.5 rounded font-medium transition-colors flex-shrink-0 ${
                    schemaChanged && parsedSchema
                      ? "bg-primary text-primary-foreground hover:bg-primary/90"
                      : "bg-secondary text-muted-foreground cursor-not-allowed opacity-50"
                  }`}
                  onClick={saveSchema}
                >
                  {schemaChanged ? "Save" : "Saved"}
                </button>
              </div>

              {/* JSON editor */}
              {editorPane === "json" && (
                <textarea
                  className="flex-1 w-full font-mono text-xs leading-relaxed p-4 resize-none focus:outline-none bg-[#060810] text-[#8ba7d8] caret-[#3d7fff]"
                  value={schemaText}
                  onChange={(e) => handleSchemaChange(e.target.value)}
                  spellCheck={false}
                />
              )}

              {/* Section builder */}
              {editorPane === "sections" && (
                parsedSchema ? (
                  <SectionBuilder
                    schema={parsedSchema}
                    onChange={handleSectionBuilderChange}
                  />
                ) : (
                  <div className="flex-1 flex items-center justify-center">
                    <p className="text-xs text-muted-foreground">Resolve JSON errors to use the section builder.</p>
                  </div>
                )
              )}
            </div>

            {/* Live preview */}
            <div className="flex-1 flex flex-col overflow-hidden min-w-0">
              <div className="px-4 py-2 border-b border-border bg-card/20 flex-shrink-0">
                <span className="font-mono text-[10px] font-semibold tracking-widest uppercase text-muted-foreground">Live Preview</span>
              </div>
              <div className="flex-1 overflow-y-auto p-5">
                {parsedSchema ? (
                  <>
                    <h2 className="text-sm font-semibold text-foreground mb-4">
                      {parsedSchema.title ?? selectedForm?.name}
                    </h2>
                    <FormRenderer schema={parsedSchema} />
                  </>
                ) : (
                  <div className="flex flex-col items-center justify-center h-32 gap-2">
                    <span className="text-danger text-xs font-mono">Invalid JSON</span>
                    <p className="text-muted-foreground text-xs">Fix the schema to see the preview.</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {/* ── Submissions ── */}
        {activeTab === "submissions" && (
          <div className="flex-1 flex flex-col overflow-hidden min-h-0">
            <div className="flex items-center gap-2 px-4 py-2.5 border-b border-border bg-card/20 flex-shrink-0 flex-wrap">
              <div className="relative">
                <span className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground">
                  <IconSearch />
                </span>
                <input
                  type="text"
                  className="bg-secondary border border-border rounded pl-8 pr-3 py-1.5 text-xs text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-ring w-52"
                  placeholder="Search by name, form, ID…"
                  value={submissionSearch}
                  onChange={(e) => setSubmissionSearch(e.target.value)}
                />
              </div>
              <select
                className="bg-secondary border border-border rounded px-2.5 py-1.5 text-xs text-foreground focus:outline-none focus:ring-1 focus:ring-ring"
                value={submissionFormFilter}
                onChange={(e) => setSubmissionFormFilter(e.target.value)}
              >
                <option value="all">All Forms</option>
                {forms.map((f) => <option key={f.id} value={f.id}>{f.name}</option>)}
              </select>
              <div className="ml-auto flex gap-1.5">
                <button className="flex items-center gap-1.5 px-2.5 py-1.5 text-[11px] font-medium text-muted-foreground border border-border rounded hover:text-foreground hover:border-primary/40 transition-colors">
                  <IconDownload /> Export XLSX
                </button>
                <button className="flex items-center gap-1.5 px-2.5 py-1.5 text-[11px] font-medium text-muted-foreground border border-border rounded hover:text-foreground hover:border-primary/40 transition-colors">
                  <IconDownload /> Export JSON
                </button>
              </div>
            </div>

            <div className="flex-1 overflow-auto">
              <table className="w-full text-xs border-collapse">
                <thead className="sticky top-0 bg-card z-10">
                  <tr className="border-b border-border">
                    {["#", "Form", "Submitted By", "Space", "Date", ""].map((h, i) => (
                      <th key={i} className={`px-4 py-2.5 text-[10px] font-bold tracking-wider uppercase text-muted-foreground ${i === 5 ? "text-right" : "text-left"}`}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {filteredSubmissions.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="px-4 py-10 text-center text-muted-foreground text-xs">
                        No submissions match your filters
                      </td>
                    </tr>
                  ) : filteredSubmissions.map((sub) => (
                    <tr key={sub.id} className="border-b border-border/40 hover:bg-secondary/25 transition-colors group">
                      <td className="px-4 py-3 font-mono text-muted-foreground">{sub.id}</td>
                      <td className="px-4 py-3 text-foreground font-medium">{sub.formName}</td>
                      <td className="px-4 py-3">
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
                      <td className="px-4 py-3">
                        <span className={`inline-flex px-1.5 py-0.5 rounded border text-[10px] font-medium ${spaceColor(sub.spaceId)}`}>
                          {sub.spaceName}
                        </span>
                      </td>
                      <td className="px-4 py-3 font-mono text-muted-foreground">{formatDate(sub.createdAt)}</td>
                      <td className="px-4 py-3 text-right">
                        <button
                          className="text-primary text-[11px] opacity-0 group-hover:opacity-100 hover:text-primary/80 transition-all"
                          onClick={() => setViewingSubmission(sub)}
                        >
                          View →
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="px-4 py-2 border-t border-border bg-card/20 flex-shrink-0">
              <span className="text-[10px] text-muted-foreground font-mono">
                {filteredSubmissions.length} of {submissions.length} submissions
              </span>
            </div>
          </div>
        )}
      </div>
    </div>

    {/* ── Unified create modal ── */}
    {createModalOpen && (
      <UnifiedCreateModal
        spaces={spaces}
        onClose={() => setCreateModalOpen(false)}
        onCreateSpace={createSpace}
        onCreateForm={createForm}
      />
    )}
    </>
  );
}
