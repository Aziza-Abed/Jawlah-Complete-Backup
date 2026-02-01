/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: {
    extend: {
      colors: {
        // Nature-Inspired Brand Palette
        primary: {
          DEFAULT: "#7895B2", // Steel Blue (Main interactive elements)
          hover: "#647E99",
        },
        secondary: {
          DEFAULT: "#A3B18A", // Olive Green (Helper/Secondary buttons)
          hover: "#8C9C72",
        },
        background: {
          DEFAULT: "#F3F1ED", // Stone Beige (Main page background)
          paper: "#F9F8F5",   // Off-white Beige (Cards & backgrounds)
        },
        surface: {
          DEFAULT: "rgba(255, 255, 255, 0.7)", // Glass effect base
          hover: "rgba(255, 255, 255, 0.9)",
        },
        text: {
          primary: "#2F2F2F",   // Dark Gray (Main text)
          secondary: "#6C757D", // Medium Gray (Subtext)
          muted: "rgba(47, 47, 47, 0.6)",
        },
        accent: {
          DEFAULT: "#C97A63", // Terracotta (Alerts & Important notes)
          glow: "rgba(201, 122, 99, 0.3)",
        },
        success: "#A3B18A", // Using Olive Green for success too as it fits the theme
        error: "#C97A63",   // Using Terracotta for error as requested for alerts
        warning: "#E9C46A", // Soft amber for warning (complementary)
      },
      fontFamily: {
        sans: ["Cairo", "sans-serif"],
      },
      animation: {
        'fade-in': 'fadeIn 0.5s ease-out',
        'slide-up': 'slideUp 0.5s ease-out',
        'slide-in-right': 'slideInRight 0.3s ease-out',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        slideUp: {
          '0%': { opacity: '0', transform: 'translateY(20px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        slideInRight: {
          '0%': { opacity: '0', transform: 'translateX(-20px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
      },
      backdropBlur: {
        xs: '2px',
      }
    },
  },
  plugins: [],
};
