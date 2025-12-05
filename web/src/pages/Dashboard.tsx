import { useEffect, useState } from "react";
import { pingApi } from "../api/health";

export default function Dashboard() {
  const [healthStatus, setHealthStatus] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const checkHealth = async () => {
      setLoading(true);
      setError(null);

      try {
        const data = await pingApi();

        // Handle possible API response structures
        if (typeof data === "string") {
          setHealthStatus(data);
        } else if (data?.status) {
          setHealthStatus(data.status);
        } else {
          setHealthStatus("API responded successfully");
        }
      } catch (err: unknown) {
        const message =
          err instanceof Error
            ? err.message
            : "Failed to reach backend API.";
        setError(message);
        console.error(err);
      } finally {
        setLoading(false);
      }
    };

    checkHealth();
  }, []);

  return (
    <div className="space-y-4 text-left">
      <h1 className="text-2xl font-bold">Dashboard</h1>

      <div className="mt-4 p-4 bg-white rounded shadow">
        <h2 className="text-lg font-semibold mb-2">Backend Health Check</h2>

        {loading && <p>Checking API health...</p>}

        {error && <p className="text-red-500">{error}</p>}

        {!loading && !error && healthStatus && (
          <p className="text-green-600">API Status: {healthStatus}</p>
        )}
      </div>
    </div>
  );
}
