import { BrowserRouter as Router, Routes, Route, useLocation, Navigate } from 'react-router-dom';
import { AnimatePresence } from 'framer-motion';
import LandingPage from './pages/LandingPage';
import Login from './pages/auth/Login';
import Register from './pages/auth/Register';
import ParticipantDashboard from './pages/participant/ParticipantDashboard';
import VolunteerDashboard from './pages/volunteer/VolunteerDashboard';
import AdminDashboard from './pages/admin/AdminDashboard';
import { GameProvider } from './context/GameContext';

function AppRoutes() {
    const location = useLocation();

    return (
        <AnimatePresence mode="wait">
            <Routes location={location} key={location.pathname}>
                <Route path="/" element={<LandingPage />} />
                <Route path="/auth/login" element={<Login />} />
                <Route path="/auth/register" element={<Register />} />
                <Route path="/mission/control" element={<ParticipantDashboard />} />
                <Route path="/volunteer" element={<VolunteerDashboard />} />
                <Route path="/admin" element={<AdminDashboard />} />
                <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
        </AnimatePresence>
    );
}

function App() {
    return (
        <GameProvider>
            <Router>
                <div className="min-h-screen bg-background text-text-main selection:bg-primary/30">
                    <AppRoutes />
                </div>
            </Router>
        </GameProvider>
    );
}

export default App;
