import type { FormSchema } from '@fms/types';

export type FormRendererProps = {
  schema: FormSchema;
};

/**
 * Renders a form from a JSON Schema definition.
 * Placeholder for feature 08 — widgets are mapped here.
 */
export function FormRenderer({ schema }: FormRendererProps) {
  return (
    <form>
      <h2>{schema.title}</h2>
    </form>
  );
}
