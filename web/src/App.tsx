import AppRoutes from "./routes/AppRoutes";
import { MunicipalityProvider } from "./contexts/MunicipalityContext";
import { ErrorBoundary } from "./components/common/ErrorBoundary";

export default function App() {
  return (
    <div className="font-sans">
      <ErrorBoundary>
        <MunicipalityProvider>
          <AppRoutes />
        </MunicipalityProvider>
      </ErrorBoundary>
    </div>
  );
}
