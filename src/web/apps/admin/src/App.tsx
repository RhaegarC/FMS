import { LoginPage, RequireAuth, RequireRole } from '@fms/auth';
import { Route, Routes } from 'react-router-dom';
import { AdminHomePage } from './pages/AdminHomePage';
import { ConfigPage } from './pages/ConfigPage';

/**
 * Admin portal routes. Everything except /login sits behind RequireAuth; the
 * /config workspace additionally requires the admin role (feature 04).
 */
function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage title="FMS Admin" />} />
      <Route element={<RequireAuth />}>
        <Route path="/" element={<AdminHomePage />} />
        <Route
          path="/config"
          element={
            <RequireRole role="admin">
              <ConfigPage />
            </RequireRole>
          }
        />
      </Route>
    </Routes>
  );
}

export default App;
