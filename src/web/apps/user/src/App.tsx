import { LoginPage, RequireAuth } from '@fms/auth';
import { APP_NAME } from '@fms/types';
import { Route, Routes } from 'react-router-dom';
import { UserHomePage } from './pages/UserHomePage';

/**
 * User portal routes. Everything except /login sits behind RequireAuth so
 * unauthenticated visitors are redirected to sign in (feature 04).
 */
function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage title={APP_NAME} />} />
      <Route element={<RequireAuth />}>
        <Route path="/" element={<UserHomePage />} />
      </Route>
    </Routes>
  );
}

export default App;
