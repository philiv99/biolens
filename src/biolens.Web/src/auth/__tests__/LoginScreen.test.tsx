import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { LoginScreen } from '../LoginScreen';
import { AuthProvider } from '../AuthContext';

// Mock the authApi module
vi.mock('../authApi', () => ({
  login: vi.fn(),
  register: vi.fn(),
  validateSession: vi.fn().mockResolvedValue(null),
  getStoredUser: vi.fn().mockReturnValue(null),
  getStoredUserId: vi.fn().mockReturnValue(null),
  storeUser: vi.fn(),
  clearAuthStorage: vi.fn(),
}));

function renderLogin() {
  return render(
    <MemoryRouter initialEntries={['/login']}>
      <AuthProvider>
        <LoginScreen />
      </AuthProvider>
    </MemoryRouter>,
  );
}

describe('LoginScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the BioLens brand', () => {
    renderLogin();
    expect(screen.getByText(/BioLens/)).toBeInTheDocument();
  });

  it('renders login form fields', () => {
    renderLogin();
    expect(screen.getByLabelText(/username/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('renders link to registration', () => {
    renderLogin();
    expect(screen.getByText(/create one/i)).toBeInTheDocument();
  });

  it('shows error on failed login', async () => {
    const { login } = await import('../authApi');
    (login as ReturnType<typeof vi.fn>).mockRejectedValue(new Error('Invalid credentials'));

    renderLogin();

    const user = userEvent.setup();
    await user.type(screen.getByLabelText(/username/i), 'baduser');
    await user.type(screen.getByLabelText(/password/i), 'badpass');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByText(/invalid credentials/i)).toBeInTheDocument();
  });

  it('disables button while loading', async () => {
    const { login } = await import('../authApi');
    (login as ReturnType<typeof vi.fn>).mockImplementation(
      () => new Promise(() => {}), // Never resolves
    );

    renderLogin();

    const user = userEvent.setup();
    await user.type(screen.getByLabelText(/username/i), 'testuser');
    await user.type(screen.getByLabelText(/password/i), 'testpass');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(screen.getByRole('button')).toBeDisabled();
  });
});
