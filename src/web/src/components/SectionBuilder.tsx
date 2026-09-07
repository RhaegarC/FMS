import { useRef, useEffect, useState } from "react";
import type { JsonSchema, SchemaSection, SectionField, SchemaProperty } from "@/data/mockData";

interface SectionBuilderProps {
  schema: JsonSchema;
  onChange: (updated: JsonSchema) => void;
}

function uid() {
  return `s-${Date.now()}-${Math.random().toString(36).slice(2, 6)}`;
}

export default function SectionBuilder({ schema, onChange }: SectionBuilderProps) {
  const properties = schema.properties ?? {};
  const sections = schema["x-sections"] ?? [];
  const allNames = Object.keys(properties);

  const assignedSet = new Set(sections.flatMap((s) => s.fields.map((f) => f.name)));
  const unassigned = allNames.filter((n) => !assignedSet.has(n));

  const setSections = (next: SchemaSection[]) => onChange({ ...schema, "x-sections": next });

  const addSection = () =>
    setSections([
      ...sections,
      { id: uid(), title: "New Section", columns: 2, fields: [] },
    ]);

  const updateSection = (id: string, patch: Partial<SchemaSection>) =>
    setSections(sections.map((s) => (s.id === id ? { ...s, ...patch } : s)));

  const setColumns = (id: string, columns: number) => {
    setSections(
      sections.map((s) =>
        s.id === id
          ? {
              ...s,
              columns,
              fields: s.fields.map((f) => ({ ...f, column: Math.min(f.column, columns) })),
            }
          : s,
      ),
    );
  };

  const deleteSection = (id: string) => setSections(sections.filter((s) => s.id !== id));

  const moveSection = (id: string, dir: "up" | "down") => {
    const idx = sections.findIndex((s) => s.id === id);
    if (dir === "up" && idx === 0) return;
    if (dir === "down" && idx === sections.length - 1) return;
    const next = [...sections];
    const swap = dir === "up" ? idx - 1 : idx + 1;
    [next[idx], next[swap]] = [next[swap], next[idx]];
    setSections(next);
  };

  const assignField = (sectionId: string, name: string, column: number) =>
    setSections(
      sections.map((s) =>
        s.id === sectionId ? { ...s, fields: [...s.fields, { name, column }] } : s,
      ),
    );

  const removeField = (sectionId: string, name: string) =>
    setSections(
      sections.map((s) =>
        s.id === sectionId ? { ...s, fields: s.fields.filter((f) => f.name !== name) } : s,
      ),
    );

  const moveField = (sectionId: string, name: string, toColumn: number) =>
    setSections(
      sections.map((s) =>
        s.id === sectionId
          ? { ...s, fields: s.fields.map((f) => (f.name === name ? { ...f, column: toColumn } : f)) }
          : s,
      ),
    );

  return (
    <div className="flex flex-col h-full overflow-hidden">
      <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-3">
        {sections.length === 0 && (
          <div className="flex flex-col items-center justify-center py-10 gap-3 border border-dashed border-border/60 rounded-md">
            <p className="text-xs text-muted-foreground">No sections defined</p>
            <button
              className="px-3 py-1.5 text-xs font-medium bg-primary/10 text-primary rounded hover:bg-primary/20 transition-colors"
              onClick={addSection}
            >
              + Add first section
            </button>
          </div>
        )}

        {sections.map((section, idx) => (
          <SectionCard
            key={section.id}
            section={section}
            properties={properties}
            unassigned={unassigned}
            isFirst={idx === 0}
            isLast={idx === sections.length - 1}
            onRename={(title) => updateSection(section.id, { title })}
            onSetColumns={(c) => setColumns(section.id, c)}
            onDelete={() => deleteSection(section.id)}
            onMove={(d) => moveSection(section.id, d)}
            onAssign={(name, col) => assignField(section.id, name, col)}
            onRemove={(name) => removeField(section.id, name)}
            onMoveField={(name, col) => moveField(section.id, name, col)}
          />
        ))}

        {sections.length > 0 && (
          <button
            className="self-start flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-muted-foreground border border-dashed border-border/60 rounded hover:text-foreground hover:border-primary/40 transition-colors"
            onClick={addSection}
          >
            <svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4" />
            </svg>
            Add section
          </button>
        )}

        {/* Unassigned fields */}
        {allNames.length > 0 && (
          <div className="mt-1">
            <p className="text-[10px] font-bold tracking-widest uppercase text-muted-foreground mb-2">
              Unassigned fields
              <span className="ml-1.5 font-mono font-normal">({unassigned.length})</span>
            </p>
            {unassigned.length === 0 ? (
              <p className="text-xs text-muted-foreground/60 italic">All fields assigned</p>
            ) : (
              <div className="flex flex-wrap gap-1.5">
                {unassigned.map((name) => (
                  <UnassignedChip
                    key={name}
                    name={name}
                    prop={properties[name]}
                    sections={sections}
                    onAssign={assignField}
                  />
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function SectionCard({
  section,
  properties,
  unassigned,
  isFirst,
  isLast,
  onRename,
  onSetColumns,
  onDelete,
  onMove,
  onAssign,
  onRemove,
  onMoveField,
}: {
  section: SchemaSection;
  properties: Record<string, SchemaProperty>;
  unassigned: string[];
  isFirst: boolean;
  isLast: boolean;
  onRename: (t: string) => void;
  onSetColumns: (c: number) => void;
  onDelete: () => void;
  onMove: (d: "up" | "down") => void;
  onAssign: (name: string, col: number) => void;
  onRemove: (name: string) => void;
  onMoveField: (name: string, col: number) => void;
}) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(section.title);
  const titleRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (editing) titleRef.current?.select();
  }, [editing]);

  const commitTitle = () => {
    if (draft.trim()) onRename(draft.trim());
    else setDraft(section.title);
    setEditing(false);
  };

  const cols = Math.max(1, Math.min(4, section.columns));
  const byColumn: SectionField[][] = Array.from({ length: cols }, (_, i) =>
    section.fields.filter((f) => f.column === i + 1),
  );

  const colLabel = ["—", "1 col", "2 cols", "3 cols", "4 cols"];

  return (
    <div className="border border-border rounded-md overflow-hidden">
      {/* Section header */}
      <div className="flex items-center gap-2 px-3 py-2 bg-secondary/40 border-b border-border">
        {/* Reorder */}
        <div className="flex flex-col gap-0.5">
          <button
            className="text-muted-foreground hover:text-foreground disabled:opacity-30 transition-colors leading-none"
            disabled={isFirst}
            onClick={() => onMove("up")}
            title="Move up"
          >
            <svg className="w-2.5 h-2.5" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M14.77 12.79a.75.75 0 01-1.06-.02L10 8.832 6.29 12.77a.75.75 0 11-1.08-1.04l4.25-4.5a.75.75 0 011.08 0l4.25 4.5a.75.75 0 01-.02 1.06z" clipRule="evenodd" />
            </svg>
          </button>
          <button
            className="text-muted-foreground hover:text-foreground disabled:opacity-30 transition-colors leading-none"
            disabled={isLast}
            onClick={() => onMove("down")}
            title="Move down"
          >
            <svg className="w-2.5 h-2.5" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clipRule="evenodd" />
            </svg>
          </button>
        </div>

        {/* Title */}
        {editing ? (
          <input
            ref={titleRef}
            className="flex-1 bg-secondary border border-primary/60 rounded px-1.5 py-0.5 text-xs text-foreground focus:outline-none focus:ring-1 focus:ring-ring"
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onBlur={commitTitle}
            onKeyDown={(e) => { if (e.key === "Enter") commitTitle(); if (e.key === "Escape") { setDraft(section.title); setEditing(false); } }}
          />
        ) : (
          <button
            className="flex-1 text-left text-xs font-semibold text-foreground hover:text-primary transition-colors truncate"
            onClick={() => { setDraft(section.title); setEditing(true); }}
            title="Click to rename"
          >
            {section.title}
          </button>
        )}

        {/* Columns selector */}
        <div className="flex items-center gap-1 flex-shrink-0">
          <span className="text-[10px] text-muted-foreground">Cols:</span>
          <select
            className="bg-secondary border border-border rounded px-1.5 py-0.5 text-[11px] text-foreground focus:outline-none focus:ring-1 focus:ring-ring"
            value={section.columns}
            onChange={(e) => onSetColumns(Number(e.target.value))}
          >
            {[1, 2, 3, 4].map((n) => <option key={n} value={n}>{colLabel[n]}</option>)}
          </select>
        </div>

        {/* Delete */}
        <button
          className="text-muted-foreground hover:text-danger transition-colors flex-shrink-0"
          onClick={onDelete}
          title="Delete section"
        >
          <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
          </svg>
        </button>
      </div>

      {/* Column grid */}
      <div
        className="grid gap-px bg-border"
        style={{ gridTemplateColumns: `repeat(${cols}, minmax(0, 1fr))` }}
      >
        {byColumn.map((colFields, colIdx) => (
          <div key={colIdx} className="bg-card flex flex-col gap-2 p-2.5 min-h-[60px]">
            <p className="text-[10px] font-bold tracking-widest uppercase text-muted-foreground/60">
              Col {colIdx + 1}
            </p>
            {colFields.map((sf) => {
              const prop = properties[sf.name];
              const otherCols = Array.from({ length: cols }, (_, i) => i + 1).filter((c) => c !== sf.column);
              return (
                <FieldChip
                  key={sf.name}
                  label={prop?.title ?? sf.name}
                  typeTag={fieldTypeTag(prop)}
                  otherCols={otherCols}
                  onRemove={() => onRemove(sf.name)}
                  onMoveTo={(col) => onMoveField(sf.name, col)}
                />
              );
            })}
            {/* Add unassigned field to this column */}
            {unassigned.length > 0 && (
              <AddFieldSelect
                unassigned={unassigned}
                properties={properties}
                onAdd={(name) => onAssign(name, colIdx + 1)}
              />
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

function FieldChip({
  label,
  typeTag,
  otherCols,
  onRemove,
  onMoveTo,
}: {
  label: string;
  typeTag: string;
  otherCols: number[];
  onRemove: () => void;
  onMoveTo: (col: number) => void;
}) {
  return (
    <div className="group flex items-center gap-1 bg-secondary border border-border/60 rounded px-2 py-1 text-[11px] text-foreground">
      <span className="w-1 h-3 rounded-sm bg-primary/40 flex-shrink-0" />
      <span className="truncate flex-1">{label}</span>
      <span className="text-[9px] font-mono text-muted-foreground/60 flex-shrink-0">{typeTag}</span>
      {/* Move to other column */}
      {otherCols.map((col) => (
        <button
          key={col}
          className="opacity-0 group-hover:opacity-100 text-[9px] text-muted-foreground hover:text-primary transition-all px-0.5"
          title={`Move to column ${col}`}
          onClick={() => onMoveTo(col)}
        >
          →{col}
        </button>
      ))}
      {/* Remove */}
      <button
        className="opacity-0 group-hover:opacity-100 text-muted-foreground hover:text-danger transition-all ml-0.5"
        onClick={onRemove}
        title="Remove from section"
      >
        <svg className="w-2.5 h-2.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>
    </div>
  );
}

function AddFieldSelect({
  unassigned,
  properties,
  onAdd,
}: {
  unassigned: string[];
  properties: Record<string, SchemaProperty>;
  onAdd: (name: string) => void;
}) {
  return (
    <select
      className="w-full bg-transparent border border-dashed border-border/50 rounded px-1.5 py-1 text-[10px] text-muted-foreground focus:outline-none focus:ring-1 focus:ring-ring hover:border-primary/40 transition-colors cursor-pointer"
      value=""
      onChange={(e) => { if (e.target.value) onAdd(e.target.value); }}
    >
      <option value="">+ Add field…</option>
      {unassigned.map((name) => (
        <option key={name} value={name}>{properties[name]?.title ?? name}</option>
      ))}
    </select>
  );
}

function UnassignedChip({
  name,
  prop,
  sections,
  onAssign,
}: {
  name: string;
  prop: SchemaProperty;
  sections: SchemaSection[];
  onAssign: (sectionId: string, name: string, col: number) => void;
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [open]);

  if (sections.length === 0) {
    return (
      <span className="px-2 py-0.5 bg-secondary border border-border/60 rounded text-[11px] text-muted-foreground">
        {prop?.title ?? name}
      </span>
    );
  }

  return (
    <div ref={ref} className="relative">
      <button
        className="flex items-center gap-1 px-2 py-0.5 bg-secondary border border-border/60 rounded text-[11px] text-muted-foreground hover:text-foreground hover:border-primary/40 transition-colors"
        onClick={() => setOpen((v) => !v)}
      >
        {prop?.title ?? name}
        <svg className="w-2.5 h-2.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4" />
        </svg>
      </button>
      {open && (
        <div className="absolute bottom-full left-0 mb-1 z-20 bg-card border border-border rounded shadow-lg py-1 min-w-[160px]">
          <p className="px-2.5 py-1 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">Assign to</p>
          {sections.map((s) =>
            Array.from({ length: s.columns }, (_, i) => i + 1).map((col) => (
              <button
                key={`${s.id}-${col}`}
                className="w-full text-left px-2.5 py-1 text-xs text-foreground hover:bg-secondary/60 transition-colors"
                onClick={() => { onAssign(s.id, name, col); setOpen(false); }}
              >
                {s.title} — Col {col}
              </button>
            )),
          )}
        </div>
      )}
    </div>
  );
}

function fieldTypeTag(prop?: SchemaProperty): string {
  if (!prop) return "?";
  if (prop.enum) return "select";
  if (prop.format === "date") return "date";
  if (prop.format === "textarea") return "text↕";
  if (prop.type === "boolean") return "bool";
  if (prop.type === "number") return "num";
  if (prop.type === "integer") return "int";
  return "text";
}
