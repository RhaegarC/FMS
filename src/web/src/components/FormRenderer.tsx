import { useState, useMemo } from "react";
import type { JsonSchema, SchemaProperty, SchemaSection } from "@/data/mockData";

interface FormRendererProps {
  schema: JsonSchema;
  readOnly?: boolean;
  initialValues?: Record<string, unknown>;
  onSubmit?: (values: Record<string, unknown>) => void;
}

export default function FormRenderer({
  schema,
  readOnly = false,
  initialValues = {},
  onSubmit,
}: FormRendererProps) {
  const [values, setValues] = useState<Record<string, unknown>>(initialValues);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [submitted, setSubmitted] = useState(false);

  const isConditionallyRequired = useMemo(() => {
    if (!schema.if?.properties || !schema.then?.required) return schema.required ?? [];
    const conditionMet = Object.entries(schema.if.properties).every(([key, cond]) => {
      if ("const" in cond) return values[key] === cond.const;
      return true;
    });
    return conditionMet ? (schema.then.required ?? schema.required ?? []) : (schema.required ?? []);
  }, [schema, values]);

  const isRequired = (name: string) => isConditionallyRequired.includes(name);

  const handleChange = (name: string, value: unknown) => {
    setValues((prev) => ({ ...prev, [name]: value }));
    if (errors[name]) setErrors((prev) => { const e = { ...prev }; delete e[name]; return e; });
  };

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};
    for (const [name, prop] of Object.entries(schema.properties ?? {})) {
      const val = values[name];
      if (isRequired(name) && (val === undefined || val === "" || val === null)) {
        newErrors[name] = `${prop.title ?? name} is required`;
      }
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validate()) { setSubmitted(true); onSubmit?.(values); }
  };

  if (submitted) {
    return (
      <div className="flex flex-col items-center justify-center py-10 gap-4">
        <div className="w-11 h-11 rounded-full bg-accent/15 border border-accent/30 flex items-center justify-center">
          <svg className="w-5 h-5 text-accent" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
          </svg>
        </div>
        <div className="text-center">
          <p className="text-sm font-semibold text-foreground">Submission recorded</p>
          <p className="text-xs text-muted-foreground mt-1">Your response has been saved successfully.</p>
        </div>
        <button
          className="text-xs text-primary hover:text-primary/80 transition-colors"
          onClick={() => { setSubmitted(false); setValues({}); }}
        >
          Submit another response
        </button>
      </div>
    );
  }

  const sections = schema["x-sections"];
  const properties = schema.properties ?? {};

  // Fields that appear in any section
  const assignedNames = new Set(sections?.flatMap((s) => s.fields.map((f) => f.name)) ?? []);
  // Fields not in any section — render after sections in a flat list
  const unassignedNames = Object.keys(properties).filter((k) => !assignedNames.has(k));

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-5">
      {schema.description && !readOnly && (
        <p className="text-xs text-muted-foreground border-l-2 border-primary/40 pl-3 py-0.5 leading-relaxed">
          {schema.description}
        </p>
      )}

      {/* Sectioned layout */}
      {sections && sections.length > 0 && sections.map((section) => (
        <SectionBlock
          key={section.id}
          section={section}
          properties={properties}
          values={values}
          errors={errors}
          readOnly={readOnly}
          isRequired={isRequired}
          onChange={handleChange}
        />
      ))}

      {/* Unassigned fields (no section) */}
      {unassignedNames.length > 0 && (
        <div className="flex flex-col gap-4">
          {sections && sections.length > 0 && (
            <p className="text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/60">Other</p>
          )}
          {unassignedNames.map((name) => {
            const prop = properties[name];
            if (!prop) return null;
            return (
              <Field
                key={name}
                name={name}
                prop={prop}
                value={values[name]}
                onChange={(v) => handleChange(name, v)}
                required={isRequired(name)}
                error={errors[name]}
                readOnly={readOnly}
              />
            );
          })}
        </div>
      )}

      {!readOnly && (
        <div className="pt-1">
          <button
            type="submit"
            className="px-5 py-2 bg-primary text-primary-foreground text-xs font-semibold rounded hover:bg-primary/90 active:scale-[0.98] transition-all"
          >
            Submit
          </button>
        </div>
      )}
    </form>
  );
}

function SectionBlock({
  section,
  properties,
  values,
  errors,
  readOnly,
  isRequired,
  onChange,
}: {
  section: SchemaSection;
  properties: Record<string, SchemaProperty>;
  values: Record<string, unknown>;
  errors: Record<string, string>;
  readOnly: boolean;
  isRequired: (name: string) => boolean;
  onChange: (name: string, val: unknown) => void;
}) {
  const cols = Math.max(1, Math.min(4, section.columns));
  // Group fields by column
  const byColumn: SectionField[][] = Array.from({ length: cols }, (_, i) =>
    section.fields.filter((f) => f.column === i + 1),
  );

  return (
    <div className="relative border border-border rounded-md pt-5 pb-4 px-4">
      <span className="absolute -top-2.5 left-3 bg-background px-1.5 text-[11px] font-semibold text-muted-foreground">
        {section.title}
      </span>
      <div
        className="grid gap-x-5 gap-y-0"
        style={{ gridTemplateColumns: `repeat(${cols}, minmax(0, 1fr))` }}
      >
        {byColumn.map((colFields, colIdx) => (
          <div key={colIdx} className="flex flex-col gap-4">
            {colFields.map((sf) => {
              const prop = properties[sf.name];
              if (!prop) return null;
              return (
                <Field
                  key={sf.name}
                  name={sf.name}
                  prop={prop}
                  value={values[sf.name]}
                  onChange={(v) => onChange(sf.name, v)}
                  required={isRequired(sf.name)}
                  error={errors[sf.name]}
                  readOnly={readOnly}
                />
              );
            })}
          </div>
        ))}
      </div>
    </div>
  );
}

// Re-export for use in SectionBuilder previews
export type { SectionField };

function Field({
  name,
  prop,
  value,
  onChange,
  required,
  error,
  readOnly,
}: {
  name: string;
  prop: SchemaProperty;
  value: unknown;
  onChange: (val: unknown) => void;
  required: boolean;
  error?: string;
  readOnly: boolean;
}) {
  const label = prop.title ?? name;
  const base =
    "w-full bg-secondary border rounded px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-ring transition-colors disabled:opacity-50 disabled:cursor-not-allowed";
  const borderClass = error ? "border-danger/60 focus:ring-danger/40" : "border-border focus:border-primary/60";

  const renderInput = () => {
    if (prop.type === "boolean") {
      const checked = Boolean(value);
      return (
        <button
          type="button"
          role="switch"
          aria-checked={checked}
          disabled={readOnly}
          onClick={() => !readOnly && onChange(!checked)}
          className={`relative w-9 h-5 rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-1 focus:ring-offset-background disabled:opacity-50 disabled:cursor-not-allowed ${
            checked ? "bg-primary" : "bg-secondary border border-border"
          }`}
        >
          <span
            className={`absolute top-0.5 left-0.5 w-4 h-4 rounded-full bg-white shadow-sm transition-transform ${
              checked ? "translate-x-4" : ""
            }`}
          />
        </button>
      );
    }
    if (prop.enum) {
      return (
        <select className={`${base} ${borderClass}`} value={String(value ?? "")} onChange={(e) => onChange(e.target.value)} disabled={readOnly}>
          <option value="">Select {label}…</option>
          {prop.enum.map((opt) => <option key={opt} value={opt}>{opt}</option>)}
        </select>
      );
    }
    if (prop.format === "textarea") {
      return (
        <textarea
          className={`${base} ${borderClass} min-h-[72px] resize-y leading-relaxed`}
          value={String(value ?? "")}
          onChange={(e) => onChange(e.target.value)}
          placeholder={prop.description ?? `Enter ${label.toLowerCase()}…`}
          disabled={readOnly}
          rows={3}
        />
      );
    }
    if (prop.format === "date") {
      return (
        <input type="date" className={`${base} ${borderClass}`} value={String(value ?? "")} onChange={(e) => onChange(e.target.value)} disabled={readOnly} />
      );
    }
    if (prop.type === "number" || prop.type === "integer") {
      return (
        <input
          type="number"
          className={`${base} ${borderClass}`}
          value={value !== undefined && value !== null ? String(value) : ""}
          onChange={(e) => onChange(prop.type === "integer" ? parseInt(e.target.value) : parseFloat(e.target.value))}
          placeholder={`Enter ${label.toLowerCase()}…`}
          disabled={readOnly}
          min={prop.minimum}
          max={prop.maximum}
        />
      );
    }
    return (
      <input
        type="text"
        className={`${base} ${borderClass}`}
        value={String(value ?? "")}
        onChange={(e) => onChange(e.target.value)}
        placeholder={prop.description ?? `Enter ${label.toLowerCase()}…`}
        disabled={readOnly}
      />
    );
  };

  return (
    <div className="flex flex-col gap-1.5">
      <label className="flex items-center gap-1 text-[11px] font-semibold tracking-wide uppercase text-muted-foreground">
        {label}
        {required && <span className="text-danger">*</span>}
      </label>
      {prop.type === "boolean" ? (
        <div className="flex items-center gap-2.5">
          {renderInput()}
          <span className="text-sm text-secondary-foreground">{value ? "Yes" : "No"}</span>
        </div>
      ) : (
        renderInput()
      )}
      {prop.description && prop.type !== "boolean" && !readOnly && (
        <p className="text-[11px] text-muted-foreground leading-relaxed">{prop.description}</p>
      )}
      {error && <p className="text-[11px] text-danger">{error}</p>}
    </div>
  );
}

// Needed by SectionBuilder
type SectionField = { name: string; column: number };
