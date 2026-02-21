import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import styles from './Layout.module.css';

export function AppLayout({ children }: { children: React.ReactNode }) {
  const { user, logout } = useAuth();
  const location = useLocation();

  const navItems = [
    { to: '/', label: 'Documents' },
    { to: '/upload', label: 'Upload' },
    { to: '/settings', label: 'Settings' },
  ];

  return (
    <div className={styles.layout}>
      <header className={styles.header}>
        <div className={styles.headerInner}>
          <Link to="/" className={styles.brand}>
            🔬 <span>BioLens</span>
          </Link>

          <nav className={styles.nav} aria-label="Main navigation">
            {navItems.map(item => (
              <Link
                key={item.to}
                to={item.to}
                className={`${styles.navLink} ${location.pathname === item.to ? styles.navLinkActive : ''}`}
              >
                {item.label}
              </Link>
            ))}
          </nav>

          <div className={styles.userSection}>
            {user && (
              <>
                <span className={styles.userName}>{user.displayName}</span>
                <button className={styles.logoutBtn} onClick={logout}>
                  Sign Out
                </button>
              </>
            )}
          </div>
        </div>
      </header>

      <main className={styles.main}>
        {children}
      </main>
    </div>
  );
}
