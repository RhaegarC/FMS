import { describe, expect, it } from 'vitest';
import { APP_NAME } from './index';

describe('shared types package', () => {
  it('exposes the platform name', () => {
    expect(APP_NAME).toBe('FMS');
  });
});
