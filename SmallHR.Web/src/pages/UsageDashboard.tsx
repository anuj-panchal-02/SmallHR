import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import ReactECharts from 'echarts-for-react';
import * as echarts from 'echarts';
import { buildApiUrl } from '../utils/api';
import { Empty } from 'antd';

type TenantRow = {
    tenantId: number;
    tenantName: string;
    employeeCount: number;
    employeeLimit: number;
    apiRequestsToday: number;
    apiLimitPerDay: number;
    storageBytesUsed: number;
    storageLimitBytes?: number | null;
    activeAlertsCount: number;
};

type DashboardOverviewDto = {
    totalTenants: number;
    totalEmployees: number;
    totalApiRequests: number;
    totalApiRequestsToday: number;
    totalStorageBytes: number;
    periodStart: string;
    periodEnd: string;
    tenants: Array<{
        tenantId: number;
        tenantName: string;
        employeeCount: number;
        employeeLimit: number;
        apiRequestsThisPeriod: number;
        apiRequestsToday: number;
        apiLimitPerDay: number;
        storageBytesUsed: number;
        storageLimitBytes?: number | null;
        activeAlertsCount: number;
    }>;
    alertsSummary: {
        totalActive: number;
        critical: number;
        high: number;
        medium: number;
        low: number;
    };
    topTenantsByUsage: Array<{
        tenantId: number;
        tenantName: string;
        employeeCount: number;
        apiRequestsThisPeriod: number;
        storageBytesUsed: number;
    }>;
};

const bytesToGB = (bytes: number) => (bytes / (1024 * 1024 * 1024)).toFixed(2);

// Color palette placeholder for future theming

export default function UsageDashboard() {
    const navigate = useNavigate();
    // Using Vite dev proxy for /api → target; no explicit base needed here
    const containerRef = useRef<HTMLDivElement | null>(null);
    const [containerReady, setContainerReady] = useState(false);
    const [dashboard, setDashboard] = useState<DashboardOverviewDto | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [startDate, setStartDate] = useState<string>(() => {
        const now = new Date();
        const d = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1));
        return d.toISOString();
    });
    const [endDate, setEndDate] = useState<string>(() => new Date().toISOString());
    const [tenantFilter, setTenantFilter] = useState<number | 'all'>('all');
    const [apiHistory, setApiHistory] = useState<{ date: string; requests: number }[] | null>(null);
    const [storageHistory, setStorageHistory] = useState<{ date: string; gb: number }[] | null>(null);

    const fetchDashboard = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const endpoint = buildApiUrl('/api/UsageMetrics/dashboard', {
                startDate: startDate ?? undefined,
                endDate: endDate ?? undefined
            });

            const res = await fetch(endpoint, { credentials: 'include' });
            const contentType = res.headers.get('content-type') || '';
            if (!res.ok) {
                const errText = await res.text();
                throw new Error(`Failed to load dashboard (${res.status}): ${errText.slice(0, 200)}`);
            }
            if (!contentType.includes('application/json')) {
                const text = await res.text();
                throw new Error(`Unexpected response (not JSON): ${text.slice(0, 200)}`);
            }
            const json = (await res.json()) as DashboardOverviewDto;
            setDashboard(json);
        } catch (e: any) {
            setError(e.message ?? 'Failed to load');
        } finally {
            setLoading(false);
        }
    }, [startDate, endDate]);

    useEffect(() => {
        fetchDashboard();
        const id = setInterval(fetchDashboard, 30_000);
        return () => clearInterval(id);
    }, [fetchDashboard]);

    // Fetch real history if endpoint is available; fallback silently on errors
    useEffect(() => {
        const fetchHistory = async () => {
            try {
                if (!dashboard) return;
                const start = dashboard.periodStart ?? startDate;
                const end = dashboard.periodEnd ?? endDate;
                const endpoint = buildApiUrl('/api/UsageMetrics/history', {
                    tenantId: tenantFilter === 'all' ? undefined : tenantFilter,
                    startDate: start,
                    endDate: end,
                    granularity: 'daily'
                });
                const res = await fetch(endpoint, { credentials: 'include' });
                if (!res.ok) throw new Error('history error');
                const contentType = res.headers.get('content-type') || '';
                if (!contentType.includes('application/json')) throw new Error('not json');
                const json = await res.json();
                const points = json?.dataPoints || json?.DataPoints || [];
                if (!Array.isArray(points)) return;
                const apiSeries = points.map((p: any) => ({
                    date: (p.timestamp || p.Timestamp || p.periodStart || p.PeriodStart || '').toString().slice(0,10),
                    requests: Number(p.apiRequests ?? p.ApiRequests ?? 0)
                }));
                const storSeries = points.map((p: any) => ({
                    date: (p.timestamp || p.Timestamp || p.periodStart || p.PeriodStart || '').toString().slice(0,10),
                    gb: Number(((p.storageBytes ?? p.StorageBytes ?? 0) / (1024*1024*1024)).toFixed(2))
                }));
                setApiHistory(apiSeries);
                setStorageHistory(storSeries);
            } catch {
                // keep placeholders; don't surface errors to UI
            }
        };
        fetchHistory();
        // re-fetch on filter/date changes
    }, [dashboard, tenantFilter, startDate, endDate]);

    // Observe container size to ensure charts render only when visible and sized
    useEffect(() => {
        const el = containerRef.current;
        if (!el) return;
        const ro = new ResizeObserver((entries) => {
            for (const entry of entries) {
                const cr = entry.contentRect;
                if (cr.width > 0 && cr.height > 0) {
                    setContainerReady(true);
                }
            }
        });
        ro.observe(el);
        // initial check
        const rect = el.getBoundingClientRect();
        if (rect.width > 0 && rect.height > 0) setContainerReady(true);
        return () => ro.disconnect();
    }, []);

    const tenantRows = useMemo<TenantRow[]>(() => {
        if (!dashboard) return [];
        const base = dashboard.tenants.map(t => ({
            tenantId: t.tenantId,
            tenantName: t.tenantName,
            employeeCount: t.employeeCount,
            employeeLimit: t.employeeLimit,
            apiRequestsToday: t.apiRequestsToday,
            apiLimitPerDay: t.apiLimitPerDay,
            storageBytesUsed: t.storageBytesUsed,
            storageLimitBytes: t.storageLimitBytes ?? null,
            activeAlertsCount: t.activeAlertsCount
        }));
        if (tenantFilter === 'all') return base;
        return base.filter(r => r.tenantId === tenantFilter);
    }, [dashboard, tenantFilter]);

    const hasAnyData = useMemo(() => {
        if (!dashboard) return false;
        return (dashboard.totalApiRequests ?? 0) > 0 || (dashboard.totalStorageBytes ?? 0) > 0 || (dashboard.totalEmployees ?? 0) > 0;
    }, [dashboard]);

    // Charts data (fallback approximation due to lack of per-day history endpoint)
    const apiRequestsSeries = useMemo(() => {
        if (apiHistory && apiHistory.length) return apiHistory;
        if (!dashboard) return [] as { date: string; requests: number }[];
        const today = new Date();
        const series: { date: string; requests: number }[] = [];
        for (let i = 29; i >= 0; i--) {
            const d = new Date(today);
            d.setDate(today.getDate() - i);
            const key = d.toISOString().slice(0, 10);
            // Put today's count on last point; others zero until history endpoint added
            const isToday = i === 0;
            series.push({ date: key, requests: isToday ? dashboard.totalApiRequestsToday : 0 });
        }
        return series;
    }, [dashboard, apiHistory]);

    const storageSeries = useMemo(() => {
        if (storageHistory && storageHistory.length) return storageHistory;
        if (!dashboard) return [] as { date: string; gb: number }[];
        const today = new Date();
        const series: { date: string; gb: number }[] = [];
        for (let i = 29; i >= 0; i--) {
            const d = new Date(today);
            d.setDate(today.getDate() - i);
            const key = d.toISOString().slice(0, 10);
            // Flat series at current GB until history endpoint added
            series.push({ date: key, gb: Number(bytesToGB(dashboard.totalStorageBytes)) });
        }
        return series;
    }, [dashboard, storageHistory]);

    const topTenantsData = useMemo(() => {
        if (!dashboard) return [] as { name: string; score: number }[];
        return dashboard.topTenantsByUsage.map((t) => {
            const employeeScore = t.employeeCount * 0.4;
            const apiScore = (t.apiRequestsThisPeriod / 1000.0) * 0.3;
            const storageScore = (t.storageBytesUsed / (1024.0 * 1024.0 * 1024.0)) * 0.2;
            const score = employeeScore + apiScore + storageScore;
            return { name: t.tenantName, score: Number(score.toFixed(2)) };
        });
    }, [dashboard]);

    const usageDistributionData = useMemo(() => {
        if (!dashboard) return [] as { name: string; value: number }[];
        return [
            { name: 'Employees', value: dashboard.totalEmployees },
            { name: 'API Requests', value: dashboard.totalApiRequests },
            { name: 'Storage (GB)', value: Number(bytesToGB(dashboard.totalStorageBytes)) }
        ];
    }, [dashboard]);

    const onRowClick = (row: TenantRow) => {
        navigate(`/admin/tenants/${row.tenantId}`);
    };

    return (
        <div ref={containerRef} style={{ padding: 24, minWidth: 0 }}>
            <h1>Usage Dashboard</h1>

            {/* Filters */}
            <div style={{ display: 'flex', gap: 12, alignItems: 'center', marginBottom: 16 }}>
                <div>
                    <label>Start Date:&nbsp;</label>
                    <input type="date" value={startDate.slice(0,10)} onChange={e => setStartDate(new Date(e.target.value).toISOString())} />
                </div>
                <div>
                    <label>End Date:&nbsp;</label>
                    <input type="date" value={endDate.slice(0,10)} onChange={e => setEndDate(new Date(e.target.value).toISOString())} />
                </div>
                <div>
                    <label>Tenant:&nbsp;</label>
                    <select value={tenantFilter} onChange={e => setTenantFilter(e.target.value === 'all' ? 'all' : Number(e.target.value))}>
                        <option value="all">All</option>
                        {dashboard?.tenants.map(t => (
                            <option key={t.tenantId} value={t.tenantId}>{t.tenantName}</option>
                        ))}
                    </select>
                </div>
                <button onClick={() => fetchDashboard()} disabled={loading}>Refresh</button>
                {error && <span style={{ color: 'red' }}>{error}</span>}
            </div>

            {/* Header cards */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12, marginBottom: 24 }}>
                <Card title="Total Tenants" value={dashboard?.totalTenants ?? 0} />
                <Card title="Total Employees" value={dashboard?.totalEmployees ?? 0} />
                <Card title="Total API Requests" value={dashboard?.totalApiRequests ?? 0} />
                <Card title="Total Storage (GB)" value={dashboard ? bytesToGB(dashboard.totalStorageBytes) : '0.00'} />
            </div>

            {/* Charts */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 24, marginBottom: 24, minWidth: 0 }}>
                <div style={{ height: 280, minWidth: 0 }}>
                    <h3>API Requests Over Time (Last 30 Days)</h3>
                    {!containerReady ? (
                        <div style={{height:'100%',display:'flex',alignItems:'center',justifyContent:'center'}}>
                            <Empty description="Loading layout..." />
                        </div>
                    ) : apiRequestsSeries.every(p => p.requests === 0) ? (
                        <div style={{height:'100%',display:'flex',alignItems:'center',justifyContent:'center'}}>
                            <Empty description="No API requests yet for selected range" />
                        </div>
                    ) : (
                        <ReactECharts
                            style={{ width: '100%', height: '100%' }}
                            option={{
                                backgroundColor: 'transparent',
                                tooltip: { trigger: 'axis', formatter: (params: any) => {
                                    const p = Array.isArray(params) ? params[0] : params;
                                    return `${p.axisValue}<br/>Requests: <b>${p.data}</b>`;
                                } },
                                grid: { left: 40, right: 16, top: 24, bottom: 30 },
                                xAxis: { type: 'category', data: apiRequestsSeries.map(p => p.date), axisTick: { show: false } },
                                yAxis: { type: 'value', splitLine: { lineStyle: { type: 'dashed' } } },
                                series: [
                                    {
                                        name: 'Requests',
                                        type: 'line',
                                        smooth: true,
                                        symbol: 'none',
                                        lineStyle: { width: 3, color: '#6366F1' },
                                        areaStyle: {
                                            color: {
                                                type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
                                                colorStops: [
                                                    { offset: 0, color: 'rgba(99,102,241,0.3)' },
                                                    { offset: 1, color: 'rgba(99,102,241,0.0)' }
                                                ]
                                            }
                                        },
                                        data: apiRequestsSeries.map(p => p.requests)
                                    }
                                ]
                            }}
                        />
                    )}
                </div>
                <div style={{ height: 280, minWidth: 0 }}>
                    <h3>Storage Usage Over Time (Last 30 Days)</h3>
                    {!containerReady ? (
                        <div style={{height:'100%',display:'flex',alignItems:'center',justifyContent:'center'}}>
                            <Empty description="Loading layout..." />
                        </div>
                    ) : storageSeries.every(p => p.gb === 0) ? (
                        <div style={{height:'100%',display:'flex',alignItems:'center',justifyContent:'center'}}>
                            <Empty description="No storage usage yet for selected range" />
                        </div>
                    ) : (
                        <ReactECharts
                            style={{ width: '100%', height: '100%' }}
                            option={{
                                backgroundColor: 'transparent',
                                tooltip: { trigger: 'axis', formatter: (params: any) => {
                                    const p = Array.isArray(params) ? params[0] : params;
                                    return `${p.axisValue}<br/>Storage: <b>${p.data} GB</b>`;
                                } },
                                grid: { left: 40, right: 16, top: 24, bottom: 30 },
                                xAxis: { type: 'category', data: storageSeries.map(p => p.date), axisTick: { show: false } },
                                yAxis: { type: 'value', name: 'GB', splitLine: { lineStyle: { type: 'dashed' } } },
                                series: [
                                    {
                                        name: 'Storage (GB)',
                                        type: 'line',
                                        smooth: true,
                                        symbol: 'none',
                                        lineStyle: { width: 3, color: '#10B981' },
                                        areaStyle: {
                                            color: {
                                                type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
                                                colorStops: [
                                                    { offset: 0, color: 'rgba(16,185,129,0.3)' },
                                                    { offset: 1, color: 'rgba(16,185,129,0.0)' }
                                                ]
                                            }
                                        },
                                        data: storageSeries.map(p => p.gb)
                                    }
                                ]
                            }}
                        />
                    )}
                </div>
                <div style={{ height: 320, minWidth: 0 }}>
                    <h3>Top 10 Tenants by Usage</h3>
                    {!containerReady ? (
                        <div style={{height:'100%',display:'flex',alignItems:'center',justifyContent:'center'}}>
                            <Empty description="Loading layout..." />
                        </div>
                    ) : topTenantsData.length === 0 ? (
                        <div style={{height:'100%',display:'flex',alignItems:'center',justifyContent:'center'}}>
                            <Empty description="No tenant usage yet" />
                        </div>
                    ) : (
                        <ReactECharts
                            style={{ width: '100%', height: '100%' }}
                            option={{
                                backgroundColor: 'transparent',
                                tooltip: { trigger: 'axis', formatter: (params: any) => {
                                    const p = Array.isArray(params) ? params[0] : params;
                                    return `${p.axisValue}<br/>Score: <b>${p.data}</b>`;
                                } },
                                grid: { left: 40, right: 16, top: 24, bottom: 60 },
                                xAxis: {
                                    type: 'category',
                                    data: topTenantsData.map(t => t.name),
                                    axisLabel: { rotate:  -20, interval: 0 }
                                },
                                yAxis: { type: 'value', name: 'Score', splitLine: { lineStyle: { type: 'dashed' } } },
                                series: [
                                    {
                                        type: 'bar',
                                    data: topTenantsData.map(t => t.score),
                                        itemStyle: {
                                            color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                                                { offset: 0, color: '#A78BFA' },
                                                { offset: 1, color: '#6366F1' }
                                            ]),
                                            borderRadius: [8,8,0,0]
                                        }
                                    }
                                ]
                            }}
                        />
                    )}
                </div>
                <div style={{ height: 320, minWidth: 0 }}>
                    <h3>Usage Distribution</h3>
                    {!containerReady ? (
                        <div style={{height:'100%',display:'flex',alignItems:'center',justifyContent:'center'}}>
                            <Empty description="Loading layout..." />
                        </div>
                    ) : usageDistributionData.length === 0 || !hasAnyData ? (
                        <div style={{height:'100%',display:'flex',alignItems:'center',justifyContent:'center'}}>
                            <Empty description="No usage data yet" />
                        </div>
                    ) : (
                        <ReactECharts
                            style={{ width: '100%', height: '100%' }}
                            option={{
                                backgroundColor: 'transparent',
                                tooltip: { trigger: 'item', formatter: (p:any) => `${p.name}: <b>${p.value}</b> (${p.percent}%)` },
                                legend: { bottom: 0 },
                                series: [
                                    {
                                        type: 'pie',
                                        radius: ['30%', '70%'],
                                        roseType: 'radius',
                                        itemStyle: { borderRadius: 6 },
                                        label: { show: true },
                                        data: usageDistributionData.map((d) => ({ value: d.value, name: d.name }))
                                    }
                                ]
                            }}
                        />
                    )}
                </div>
            </div>

            {/* Tenant table */}
            <div>
                <h3>Tenant Usage Breakdown</h3>
                <div style={{ overflowX: 'auto' }}>
                    <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                        <thead>
                            <tr>
                                <Th>Tenant</Th>
                                <Th>Employees</Th>
                                <Th>API (today)</Th>
                                <Th>Storage</Th>
                                <Th>Alerts</Th>
                                <Th>Actions</Th>
                            </tr>
                        </thead>
                        <tbody>
                            {tenantRows.map((r) => (
                                <tr key={r.tenantId} style={{ cursor: 'pointer' }} onClick={() => onRowClick(r)}>
                                    <Td>{r.tenantName}</Td>
                                    <Td>{r.employeeCount} / {r.employeeLimit}</Td>
                                    <Td>{r.apiRequestsToday} / {r.apiLimitPerDay}</Td>
                                    <Td>{bytesToGB(r.storageBytesUsed)} GB{r.storageLimitBytes ? ` / ${bytesToGB(r.storageLimitBytes)} GB` : ''}</Td>
                                    <Td>{r.activeAlertsCount}</Td>
                                    <Td>
                                        <button onClick={(e) => { e.stopPropagation(); onRowClick(r); }}>View</button>
                                    </Td>
                                </tr>
                            ))}
                            {!tenantRows.length && (
                                <tr>
                                    <Td colSpan={6} style={{ textAlign: 'center', padding: 16 }}>No data</Td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
}

function Card(props: { title: string; value: number | string }) {
    return (
        <div style={{ border: '1px solid #eee', borderRadius: 8, padding: 16 }}>
            <div style={{ fontSize: 12, color: '#666' }}>{props.title}</div>
            <div style={{ fontSize: 24, fontWeight: 600 }}>{props.value}</div>
        </div>
    );
}

function Th(props: React.PropsWithChildren<React.ThHTMLAttributes<HTMLTableCellElement>>) {
    return (
        <th {...props} style={{
            textAlign: 'left',
            borderBottom: '1px solid #eee',
            padding: '8px 12px',
            whiteSpace: 'nowrap'
        }} />
    );
}

function Td(props: React.PropsWithChildren<React.TdHTMLAttributes<HTMLTableCellElement>>) {
    return (
        <td {...props} style={{
            borderBottom: '1px solid #f5f5f5',
            padding: '10px 12px',
            whiteSpace: 'nowrap'
        }} />
    );
}


