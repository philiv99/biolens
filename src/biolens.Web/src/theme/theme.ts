/** Theme tokens for BioLens */
export const theme = {
  colors: {
    primary: '#2563eb',        // Blue 600
    primaryHover: '#1d4ed8',   // Blue 700
    primaryLight: '#dbeafe',   // Blue 100
    secondary: '#7c3aed',     // Violet 600
    secondaryLight: '#ede9fe', // Violet 100
    success: '#16a34a',
    successLight: '#dcfce7',
    warning: '#d97706',
    warningLight: '#fef3c7',
    danger: '#dc2626',
    dangerLight: '#fee2e2',
    background: '#f8fafc',     // Slate 50
    surface: '#ffffff',
    surfaceHover: '#f1f5f9',   // Slate 100
    border: '#e2e8f0',         // Slate 200
    borderFocus: '#2563eb',
    text: '#0f172a',           // Slate 900
    textSecondary: '#475569',  // Slate 600
    textMuted: '#94a3b8',      // Slate 400
    textInverse: '#ffffff',
  },
  spacing: {
    xs: '0.25rem',
    sm: '0.5rem',
    md: '1rem',
    lg: '1.5rem',
    xl: '2rem',
    xxl: '3rem',
  },
  radius: {
    sm: '0.375rem',
    md: '0.5rem',
    lg: '0.75rem',
    xl: '1rem',
    full: '9999px',
  },
  fontSize: {
    xs: '0.75rem',
    sm: '0.875rem',
    base: '1rem',
    lg: '1.125rem',
    xl: '1.25rem',
    '2xl': '1.5rem',
    '3xl': '1.875rem',
  },
  shadow: {
    sm: '0 1px 2px rgba(0,0,0,0.05)',
    md: '0 4px 6px -1px rgba(0,0,0,0.1)',
    lg: '0 10px 15px -3px rgba(0,0,0,0.1)',
  },
} as const;
