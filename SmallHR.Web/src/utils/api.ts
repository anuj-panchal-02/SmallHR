export function getApiBase(): string {
  // Prefer Vite env in ESM builds
  try {
    const viteBase = (typeof import.meta !== 'undefined' && (import.meta as any)?.env?.VITE_API_BASE) as string | undefined;
    if (viteBase && typeof viteBase === 'string' && viteBase.trim().length > 0) {
      return viteBase.trim().replace(/\/$/, '');
    }
  } catch {}

  // Fallback to window global if provided by host page
  const winBase = (window as any)?.__API_BASE as string | undefined;
  if (winBase && typeof winBase === 'string' && winBase.trim().length > 0) {
    return winBase.trim().replace(/\/$/, '');
  }

  // Default: empty string so relative /api works via Vite proxy or same-origin
  return '';
}

export function buildApiUrl(path: string, params?: Record<string, string | number | boolean | undefined | null>): string {
  const base = getApiBase();
  const qs = new URLSearchParams();
  if (params) {
    Object.entries(params).forEach(([k, v]) => {
      if (v === undefined || v === null) return;
      qs.set(k, String(v));
    });
  }
  const query = qs.toString();
  const p = path.startsWith('/') ? path : `/${path}`;
  const url = base ? `${base}${p}` : p;
  return query ? `${url}?${query}` : url;
}


