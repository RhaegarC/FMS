import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import App from './App';

describe('Admin App', () => {
  it('renders the admin heading', () => {
    render(<App />);
    expect(screen.getByRole('heading', { name: 'FMS Admin' })).toBeInTheDocument();
  });

  it('renders a create-form button', () => {
    render(<App />);
    expect(screen.getByRole('button', { name: 'Create form' })).toBeInTheDocument();
  });
});
