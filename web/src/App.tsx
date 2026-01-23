import AppRoutes from "./routes/AppRoutes";
import { MunicipalityProvider } from "./contexts/MunicipalityContext";

export default function App() {
  return (
    <MunicipalityProvider>
      <AppRoutes />
    </MunicipalityProvider>
  );
}
