import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { FormRenderer } from './FormRenderer';

describe('FormRenderer', () => {
  it('renders the schema title', () => {
    render(<FormRenderer schema={{ title: 'Feedback form', properties: {} }} />);
    expect(screen.getByRole('heading', { name: 'Feedback form' })).toBeInTheDocument();
  });
});
