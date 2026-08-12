import { describe, expect, it } from 'vitest';
import { API_VERSION, buildUrl } from './index';

describe('buildUrl', () => {
  it('prepends the API version prefix', () => {
    expect(buildUrl('/health')).toBe(`/api/${API_VERSION}/health`);
  });

  it('adds a leading slash when missing', () => {
    expect(buildUrl('health')).toBe(`/api/${API_VERSION}/health`);
  });
});
