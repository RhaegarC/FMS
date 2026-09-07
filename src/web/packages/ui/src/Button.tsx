import type { ButtonHTMLAttributes, ReactNode } from 'react';

export type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  children?: ReactNode;
};

/** Base button component shared across FMS apps. */
export function Button({ children, ...rest }: ButtonProps) {
  return (
    <button type="button" {...rest}>
      {children}
    </button>
  );
}
