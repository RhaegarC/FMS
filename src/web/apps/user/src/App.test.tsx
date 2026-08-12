import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import App from './App';

describe('User App', () => {
  it('renders the platform heading', () => {
    render(<App />);
    expect(screen.getByRole('heading', { name: 'FMS' })).toBeInTheDocument();
  });

  it('renders the feedback form title via the renderer', () => {
    render(<App />);
    expect(screen.getByRole('heading', { name: 'Feedback form' })).toBeInTheDocument();
  });
});
