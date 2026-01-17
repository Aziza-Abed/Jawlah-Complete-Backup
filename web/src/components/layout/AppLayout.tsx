import { Outlet } from "react-router-dom";
import Sidebar from "./Sidebar";
import Topbar from "./Topbar";

export default function AppLayout() {
  return (
    <div dir="rtl" className="h-screen w-screen bg-background overflow-hidden">
      {/* Full page layout */}
      <div className="h-full flex flex-col">
        {/* Topbar full width */}
        <div className="shrink-0 w-full">
          <Topbar />
        </div>

          <div dir="ltr"  className="flex-1 min-h-0 flex overflow-hidden">
          <main className="flex-1 min-w-0 min-h-0 overflow-y-auto overflow-x-hidden">
            <div dir="rtl" >
              <Outlet />
            </div>
          </main>

          {/* Sidebar right */}
          <div dir="rtl" className="w-[250px] shrink-0 h-full">
            <Sidebar />
          </div>
        </div>
      </div>
    </div>
  );
}
