import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const generatedPath = fileURLToPath(new URL('./generated.ts', import.meta.url));

describe('generated API client (from OpenAPI spec)', () => {
  it('has a generated module', () => {
    const src = readFileSync(generatedPath, 'utf8');
    expect(src).toContain('paths');
  });

  it('describes the /health endpoint', () => {
    const src = readFileSync(generatedPath, 'utf8');
    expect(src).toContain('/health');
  });
});
