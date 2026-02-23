import AppRoutes from "./routes/AppRoutes";
import { MunicipalityProvider } from "./contexts/MunicipalityContext";
import { NotificationProvider } from "./contexts/NotificationContext";
import { ErrorBoundary } from "./components/common/ErrorBoundary";

export default function App() {
  return (
    <div className="font-sans">
      <ErrorBoundary>
        <MunicipalityProvider>
          <NotificationProvider>
            <AppRoutes />
          </NotificationProvider>
        </MunicipalityProvider>
      </ErrorBoundary>
    </div>
  );
}
