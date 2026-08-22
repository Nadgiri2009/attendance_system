import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./app/**/*.{ts,tsx}",
    "./components/**/*.{ts,tsx}",
    "./lib/**/*.{ts,tsx}"
  ],
  theme: {
    extend: {
      colors: {
        ink: "#0F172A",
        surface: "#F7F8FA",
        panel: "#FFFFFF",
        border: "#E4E7EC",
        primary: {
          DEFAULT: "#14385E",
          50: "#EAF0F6",
          100: "#CBDBE9",
          400: "#2E5D8A",
          600: "#14385E",
          700: "#0F2A47"
        },
        accent: {
          DEFAULT: "#C97A3A",
          light: "#F4E3D3"
        },
        success: "#1E8E5A",
        danger: "#C13A3A",
        warn: "#B7791F"
      },
      fontFamily: {
        display: ["var(--font-display)", "serif"],
        sans: ["var(--font-sans)", "sans-serif"],
        mono: ["var(--font-mono)", "monospace"]
      },
      boxShadow: {
        card: "0 1px 2px rgba(15, 23, 42, 0.06), 0 1px 3px rgba(15, 23, 42, 0.08)"
      }
    }
  },
  plugins: []
};

export default config;
