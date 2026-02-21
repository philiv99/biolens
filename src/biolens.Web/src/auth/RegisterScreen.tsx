import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from './AuthContext';
import styles from './Auth.module.css';

export function RegisterScreen() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({
    username: '',
    email: '',
    displayName: '',
    password: '',
    confirmPassword: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const update = (key: keyof typeof form, value: string) =>
    setForm(prev => ({ ...prev, [key]: value }));

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');

    if (form.password !== form.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    setLoading(true);
    try {
      await register(form);
      navigate('/', { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.card}>
        <div className={styles.brand}>
          <h1 className={styles.title}>🔬 BioLens</h1>
          <p className={styles.subtitle}>Biographical Data Extraction</p>
        </div>

        <form onSubmit={handleSubmit} className={styles.form}>
          <h2 className={styles.formTitle}>Create Account</h2>

          {error && (
            <div className={styles.error} role="alert" aria-live="assertive">
              {error}
            </div>
          )}

          <div className={styles.field}>
            <label htmlFor="reg-username" className={styles.label}>Username</label>
            <input
              id="reg-username"
              type="text"
              className={styles.input}
              value={form.username}
              onChange={e => update('username', e.target.value)}
              required
              autoComplete="username"
              autoFocus
            />
          </div>

          <div className={styles.field}>
            <label htmlFor="reg-email" className={styles.label}>Email</label>
            <input
              id="reg-email"
              type="email"
              className={styles.input}
              value={form.email}
              onChange={e => update('email', e.target.value)}
              required
              autoComplete="email"
            />
          </div>

          <div className={styles.field}>
            <label htmlFor="reg-displayName" className={styles.label}>Display Name</label>
            <input
              id="reg-displayName"
              type="text"
              className={styles.input}
              value={form.displayName}
              onChange={e => update('displayName', e.target.value)}
              required
            />
          </div>

          <div className={styles.field}>
            <label htmlFor="reg-password" className={styles.label}>Password</label>
            <input
              id="reg-password"
              type="password"
              className={styles.input}
              value={form.password}
              onChange={e => update('password', e.target.value)}
              required
              autoComplete="new-password"
            />
          </div>

          <div className={styles.field}>
            <label htmlFor="reg-confirm" className={styles.label}>Confirm Password</label>
            <input
              id="reg-confirm"
              type="password"
              className={styles.input}
              value={form.confirmPassword}
              onChange={e => update('confirmPassword', e.target.value)}
              required
              autoComplete="new-password"
            />
          </div>

          <button type="submit" className={styles.button} disabled={loading}>
            {loading ? 'Creating account…' : 'Create Account'}
          </button>

          <p className={styles.switchText}>
            Already have an account?{' '}
            <Link to="/login" className={styles.link}>Sign in</Link>
          </p>
        </form>
      </div>
    </div>
  );
}
