/** Application-wide shared constants and domain types. */

/** Display name for the whole platform. */
export const APP_NAME = 'FMS';

/**
 * A JSON Schema-based form definition (spec: docs/PRD.md).
 * Enriched as later features land (widgets, conditionals, lookups).
 */
export type FormSchema = {
  title: string;
  properties: Record<string, unknown>;
};

/** Generic envelope for API JSON responses. */
export type ApiResponse<T> = {
  data: T;
};
