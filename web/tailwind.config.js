/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: {
    extend: {
      colors: {
        primary: "#2563EB",
        background: "#F8FAFC",
        surface: "#FFFFFF",
        text: "#0F172A",
        muted: "#64748B",
        success: "#16A34A",
        error: "#DC2626",
        warning: "#F59E0B",
        disabled: "#CBD5E1",

        sidebarBg: "#7895B2",
        sidebarActiveBg: "#F3F1ED",
        sidebarInactiveText: "#A7ACB1",
        sidebarActiveText: "#2F2F2F",
      },
      fontFamily: {
        sans: ["Cairo", "sans-serif"],
      },
    },
  },
  plugins: [],
};
