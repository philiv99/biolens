import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from '../auth/AuthContext';
import { ProtectedRoute } from '../auth/ProtectedRoute';
import { LoginScreen } from '../auth/LoginScreen';
import { RegisterScreen } from '../auth/RegisterScreen';
import { AppLayout } from './AppLayout';
import { DocumentListScreen } from '../ui/screens/DocumentListScreen';
import { UploadScreen } from '../ui/screens/UploadScreen';
import { ExtractionResultsScreen } from '../ui/screens/ExtractionResultsScreen';
import { SettingsScreen } from '../ui/screens/SettingsScreen';

export function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* Public routes */}
          <Route path="/login" element={<LoginScreen />} />
          <Route path="/register" element={<RegisterScreen />} />

          {/* Protected routes */}
          <Route
            path="/*"
            element={
              <ProtectedRoute>
                <AppLayout>
                  <Routes>
                    <Route path="/" element={<DocumentListScreen />} />
                    <Route path="/upload" element={<UploadScreen />} />
                    <Route path="/documents/:id" element={<ExtractionResultsScreen />} />
                    <Route path="/settings" element={<SettingsScreen />} />
                  </Routes>
                </AppLayout>
              </ProtectedRoute>
            }
          />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
