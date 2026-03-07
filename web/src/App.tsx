import AppRoutes from "./routes/AppRoutes";
import { MunicipalityProvider } from "./contexts/MunicipalityContext";
import { NotificationProvider } from "./contexts/NotificationContext";
import { ToastProvider } from "./contexts/ToastContext";
import { ErrorBoundary } from "./components/common/ErrorBoundary";

export default function App() {
  return (
    <div className="font-sans">
      <ErrorBoundary>
        <MunicipalityProvider>
          <NotificationProvider>
            <ToastProvider>
              <AppRoutes />
            </ToastProvider>
          </NotificationProvider>
        </MunicipalityProvider>
      </ErrorBoundary>
    </div>
  );
}
