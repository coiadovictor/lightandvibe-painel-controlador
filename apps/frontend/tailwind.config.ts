import type { Config } from 'tailwindcss';

/**
 * Paleta Light & Vibe — extraída da identidade visual (lightandvibe.com + logo).
 *  - brand (indigo): cor primária dos CTAs e elementos ativos (botão "Fale Conosco" / "Descubra")
 *  - accent-green: verde neon do símbolo "&" e dos olhos do mascote
 *  - accent-blue:  azul do wordmark "Vibe"
 *  - ink: cinza-quase-preto usado nos títulos do site
 */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        brand: {
          50: '#eef0ff',
          100: '#dee1ff',
          200: '#c0c5ff',
          300: '#9aa0ff',
          400: '#7a7ef5',
          500: '#5b5bd6', // primária (CTA indigo do site)
          600: '#4f46e5',
          700: '#3f37c4',
          800: '#332d9b',
          900: '#1e1a5e',
        },
        accent: {
          green: '#39d353', // verde neon do "&" + olhos do mascote
          'green-soft': '#d6f7dd',
          blue: '#3b82f6', // azul do "Vibe"
          'blue-soft': '#dbeafe',
        },
        ink: {
          DEFAULT: '#0f172a',
          soft: '#1f2937',
          muted: '#64748b',
        },
        canvas: '#f3f4f6', // fundo claro do site
      },
      boxShadow: {
        card: '0 1px 2px rgba(15, 23, 42, 0.04), 0 1px 3px rgba(15, 23, 42, 0.06)',
      },
    },
  },
  plugins: [],
} satisfies Config;
