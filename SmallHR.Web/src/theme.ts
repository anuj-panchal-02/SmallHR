import type { ThemeConfig } from 'antd';

export type Palette = {
  primary: string;      // brand/dominant
  secondary: string;    // accents/interactive hovers
  surface: string;      // backgrounds/cards
  subtle: string;       // borders/alt backgrounds
};

export const defaultPalette: Palette = {
  primary: '#424874',   // deep indigo
  secondary: '#A6B1E1', // soft indigo
  surface: '#ffffff',   // card background / sidebar - pure white
  subtle: '#e6e6e6',    // borders / separators - light neutral gray
};

// Optional presets (can add more later without code changes)
export const presetPalettes: Record<string, Palette> = {
  default: defaultPalette,
  slate: { primary: '#334155', secondary: '#94A3B8', surface: '#F8FAFC', subtle: '#E2E8F0' },
  emerald: { primary: '#065F46', secondary: '#6EE7B7', surface: '#F0FFF4', subtle: '#DCFCE7' },
};

export const PALETTE_STORAGE_KEY = 'ui_palette';

export function getStoredPaletteOrDefault(): Palette {
  try {
    const raw = localStorage.getItem(PALETTE_STORAGE_KEY);
    if (!raw) return defaultPalette;
    const parsed = JSON.parse(raw);
    if (parsed && parsed.primary && parsed.secondary && parsed.surface && parsed.subtle) {
      return parsed as Palette;
    }
    if (typeof raw === 'string' && presetPalettes[raw]) return presetPalettes[raw];
  } catch {}
  return defaultPalette;
}

export function setActivePalette(paletteOrName: Palette | string): void {
  const palette = typeof paletteOrName === 'string' ? (presetPalettes[paletteOrName] || defaultPalette) : paletteOrName;
  try { localStorage.setItem(PALETTE_STORAGE_KEY, JSON.stringify(palette)); } catch {}
  // Notify app to re-theme
  window.dispatchEvent(new CustomEvent('palettechange', { detail: palette }));
}

export function registerGlobalPaletteAPI(): void {
  // Allow quick switching via browser console: setPalette('slate') or setPalette({ ... })
  (window as any).setPalette = (p: any) => setActivePalette(p);
}

export function getAntTheme(palette: Palette): ThemeConfig {
  return {
    token: {
      colorPrimary: palette.primary,
      colorInfo: palette.secondary,
      colorBgBase: palette.surface,
      colorBgContainer: palette.surface,
      colorBorder: palette.subtle,
      colorBorderSecondary: palette.subtle,
      borderRadius: 8,
      borderRadiusLG: 10,
      borderRadiusSM: 6,
      fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', sans-serif",
      fontSize: 14,
    },
    components: {
      Button: {
        controlHeight: 38,
        controlHeightLG: 42,
        primaryShadow: '0 4px 12px rgba(66, 72, 116, 0.3)',
      },
      Card: {
        borderRadiusLG: 12,
        colorBgContainer: palette.surface,
      },
      Table: {
        headerBg: palette.subtle,
      },
      Input: {
        controlHeight: 38,
        controlHeightLG: 42,
      }
    },
  };
}

// ---------- Accessibility helpers ----------
function hexToRgb(hex: string): { r: number; g: number; b: number } {
  const clean = hex.replace('#', '');
  const bigint = parseInt(clean.length === 3 ? clean.split('').map(c => c + c).join('') : clean, 16);
  const r = (bigint >> 16) & 255;
  const g = (bigint >> 8) & 255;
  const b = bigint & 255;
  return { r, g, b };
}

function relativeLuminance(hex: string): number {
  const { r, g, b } = hexToRgb(hex);
  const srgb = [r, g, b].map(v => {
    const c = v / 255;
    return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
  });
  return 0.2126 * srgb[0] + 0.7152 * srgb[1] + 0.0722 * srgb[2];
}

export function contrastRatio(foreground: string, background: string): number {
  const L1 = relativeLuminance(foreground);
  const L2 = relativeLuminance(background);
  const lighter = Math.max(L1, L2);
  const darker = Math.min(L1, L2);
  return (lighter + 0.05) / (darker + 0.05);
}

export function bestTextOn(bg: string, light = '#FFFFFF', dark = '#0F172A'): string {
  // Choose the text color that yields the higher contrast vs background
  const crLight = contrastRatio(light, bg);
  const crDark = contrastRatio(dark, bg);
  return crLight >= crDark ? light : dark;
}

// ---------- Semantic tokens ----------
export type SemanticColors = {
  bgBase: string;
  bgSurface: string;
  bgSidebar: string;
  border: string;
  textPrimary: string;
  textSecondary: string;
  focusRing: string;
  primary: string;
  primaryTextOn: string;
  accent: string;
  success: string;
  warning: string;
  error: string;
};

export function buildSemanticColors(palette: Palette): SemanticColors {
  const bgBase = '#f9f9fb'; // app canvas - very light gray with warm tone
  const bgSurface = palette.surface;
  const bgSidebar = '#F7F7F7'; // light gray sidebar
  const border = palette.subtle;
  const primary = palette.primary;
  const primaryTextOn = bestTextOn(primary);

  // Derive neutrals from palette for good readability
  const textPrimary = '#0F172A';
  const textSecondary = '#475569';
  const focusRing = `${palette.secondary}66`; // 40% alpha ring

  // Status colors: keep readable defaults while harmonizing with palette hue family
  const success = '#16A34A';
  const warning = '#D97706';
  const error = '#DC2626';
  const accent = palette.secondary;

  return {
    bgBase,
    bgSurface,
    bgSidebar,
    border,
    textPrimary,
    textSecondary,
    focusRing,
    primary,
    primaryTextOn,
    accent,
    success,
    warning,
    error,
  };
}

// Dark mode semantic tokens – neutral ramps tuned for dark surfaces (brand preserved)
export function buildSemanticColorsDark(palette: Palette): SemanticColors {
  const bgBase = '#0F172A';
  const bgSurface = '#1E293B';
  const bgSidebar = '#1E293B'; // Same as surface in dark mode
  const border = 'rgba(148, 163, 184, 0.28)';
  const primary = palette.primary;
  const primaryTextOn = bestTextOn(primary, '#FFFFFF', '#E2E8F0');
  const textPrimary = '#F1F5F9';
  const textSecondary = '#CBD5E1';
  const focusRing = `${palette.secondary}55`;
  const success = '#34D399';
  const warning = '#FBBF24';
  const error = '#F87171';
  const accent = palette.secondary;
  return {
    bgBase,
    bgSurface,
    bgSidebar,
    border,
    textPrimary,
    textSecondary,
    focusRing,
    primary,
    primaryTextOn,
    accent,
    success,
    warning,
    error,
  };
}

export function applyCssVariables(sem: SemanticColors): void {
  const root = document.documentElement;
  // helpers to create alpha from hex
  const toRgba = (hex: string, alpha: number) => {
    const { r, g, b } = hexToRgb(hex);
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
  };
  const vars: Record<string, string> = {
    '--color-background': sem.bgBase,
    '--color-surface': sem.bgSurface,
    '--color-sidebar': sem.bgSidebar, // Light gray sidebar
    '--color-border': sem.border, // Explicit border color
    '--color-text-primary': sem.textPrimary,
    '--color-text-secondary': sem.textSecondary,
    '--color-primary': sem.primary,
    '--color-accent': sem.accent,
    '--color-success': sem.success,
    '--color-warning': sem.warning,
    '--color-error': sem.error,
    '--glass-border': sem.border,
    // focus outline uses accent ring
    '--focus-ring': sem.focusRing,
    // alpha utilities (used by hover, shadows, statuses)
    '--primary-08a': toRgba(sem.primary, 0.08),
    '--primary-12a': toRgba(sem.primary, 0.12),
    '--primary-20a': toRgba(sem.primary, 0.20),
    '--accent-12a': toRgba(sem.accent, 0.12),
    '--border-40a': toRgba(typeof sem.border === 'string' ? sem.border : '#CBD5E1', 0.40),
    '--success-12a': toRgba(sem.success, 0.12),
    '--warning-12a': toRgba(sem.warning, 0.12),
    '--error-12a': toRgba(sem.error, 0.12),
  };
  Object.entries(vars).forEach(([k, v]) => root.style.setProperty(k, v));
}

// ---------- AntD Theme from semantic tokens ----------
export function getAntThemeFromSemantic(sem: SemanticColors): ThemeConfig {
  return {
    token: {
      colorPrimary: sem.primary,
      colorInfo: sem.accent,
      colorSuccess: sem.success,
      colorWarning: sem.warning,
      colorError: sem.error,
      colorTextBase: sem.textPrimary,
      colorText: sem.textPrimary,
      colorTextSecondary: sem.textSecondary,
      colorLink: sem.primary,
      colorLinkHover: sem.accent,
      colorBorder: sem.border,
      colorBorderSecondary: sem.border,
      colorBgBase: sem.bgBase,
      colorBgLayout: sem.bgBase,
      colorBgContainer: sem.bgSurface,
      colorFillSecondary: sem.border,
      borderRadius: 8,
      borderRadiusLG: 10,
      borderRadiusSM: 6,
      fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', sans-serif",
      fontSize: 14,
    },
    components: {
      Button: {
        controlHeight: 38,
        controlHeightLG: 42,
        primaryShadow: '0 4px 12px rgba(66, 72, 116, 0.3)'
      },
      Input: {
        controlHeight: 38,
        controlHeightLG: 42,
        activeBorderColor: sem.primary,
        hoverBorderColor: sem.primary,
      },
      Select: {
        controlHeight: 38,
        controlHeightLG: 42,
        optionSelectedBg: `${sem.primary}14`,
      },
      Table: {
        headerBg: sem.border,
        colorBorderSecondary: sem.border,
      },
      Card: {
        borderRadiusLG: 12,
        colorBgContainer: sem.bgSurface,
      },
      Tabs: {
        itemHoverColor: sem.primary,
        itemSelectedColor: sem.primary,
        inkBarColor: sem.primary,
      },
      Switch: {
        colorPrimary: sem.primary,
      },
      Alert: {
        colorSuccess: sem.success,
        colorWarning: sem.warning,
        colorError: sem.error,
        colorInfo: sem.accent,
      },
      Message: {
        colorSuccess: sem.success,
        colorWarning: sem.warning,
        colorError: sem.error,
        colorInfo: sem.accent,
      },
      Modal: {
        colorBgMask: '#00000033',
      },
    },
  };
}

// Convenience: build full ThemeConfig directly from a Palette
export function buildThemeConfig(palette: Palette): ThemeConfig {
  const sem = buildSemanticColors(palette);
  return getAntThemeFromSemantic(sem);
}


