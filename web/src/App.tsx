import { Outlet, Link } from "react-router-dom";

export default function App() {
  return (
    <div className="min-h-screen flex bg-gray-100">

      {/* Sidebar */}
      <aside className="w-60 bg-white shadow-md p-5">
        <h2 className="text-xl font-bold mb-6">جــوالــة – المشرف</h2>

        <nav className="flex flex-col gap-3 text-right">
          <Link to="/dashboard" className="hover:text-blue-600">اللوحة الرئيسية</Link>
          <Link to="/tasks" className="hover:text-blue-600">المهام</Link>
          <Link to="/zones" className="hover:text-blue-600">المناطق (Zones)</Link>
          <Link to="/login" className="text-red-500 mt-4">تسجيل خروج (Placeholder)</Link>
        </nav>
      </aside>

      {/* Main Content */}
      <main className="flex-1 p-10">
        <Outlet />
      </main>
    </div>
  );
}
