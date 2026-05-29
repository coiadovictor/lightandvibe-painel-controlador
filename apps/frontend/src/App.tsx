import { Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from './components/layout/AppLayout';
import { DashboardPage } from './pages/DashboardPage';
import { EmployeesPage } from './pages/EmployeesPage';
import { InformationTypesPage } from './pages/InformationTypesPage';
import { LogsPage } from './pages/LogsPage';
import { ProfilePage } from './pages/ProfilePage';
import { HollerithPage } from './pages/HollerithPage';
import { IntegracoesPage } from './pages/IntegracoesPage';
import { LoginPage } from './pages/LoginPage';
import { useAuth } from './contexts/AuthContext';
import type { ReactNode } from 'react';

function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
}

export default function App() {
  const { isAuthenticated } = useAuth();

  return (
    <Routes>
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to="/" replace /> : <LoginPage />}
      />
      <Route
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route path="/" element={<DashboardPage />} />
        <Route path="/logs" element={<LogsPage />} />
        <Route path="/employees" element={<EmployeesPage />} />
        <Route path="/information-types" element={<InformationTypesPage />} />
        <Route path="/hollerith" element={<HollerithPage />} />
        <Route path="/integracoes" element={<IntegracoesPage />} />
        <Route path="/profile" element={<ProfilePage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
