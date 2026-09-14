import React, { useEffect, useState } from "react";

function formatMoney(n) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", maximumFractionDigits: 0 }).format(n);
}

export default function App() {
  const [tenders, setTenders] = useState([]); // { externalId, name }
  const [selected, setSelected] = useState("");
  const [summary, setSummary] = useState({ totalSavings: 0, topBuyers: [], topSuppliers: [] });
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  useEffect(() => {
    loadTenders();
  }, []);

  useEffect(() => {
    if (selected) fetchSummary(selected);
  }, [selected]);

  async function loadTenders() {
    try {
      const res = await fetch(`/api/tenders?page=1&pageSize=100`);
      if (!res.ok) throw new Error("failed to load tenders");
      const data = await res.json();
      // API returns { Page, PageSize, Items }
      const items = data?.Items || data?.items || data?.Data || data?.data || [];
      // map to { externalId, name }
      const mapped = items.map((it) => ({ externalId: it.externalId || it.ExternalId || it.external_id || it.id, name: it.name || it.Name || it.Name || it.Name }));
      setTenders(mapped);
      if (mapped.length > 0) setSelected(mapped[0].externalId);
    } catch (e) {
      setMessage("Could not load tenders from API; using fallback sample list.");
      const fallback = [
        { externalId: "id-1", name: "Sample 1" },
        { externalId: "id-2", name: "Sample 2" },
      ];
      setTenders(fallback);
      setSelected(fallback[0].externalId);
    }
  }

  async function fetchSummary(externalId) {
    setLoading(true);
    setMessage("");
    try {
      // fetch savings for selected tender
      const savingsRes = await fetch(`/api/analytics/tender/${encodeURIComponent(externalId)}/savings`);
      if (savingsRes.ok) {
        const s = await savingsRes.json();
        // controller returns { ExternalId, BudgetSavings }
        setSummary((prev) => ({ ...prev, totalSavings: Number(s?.BudgetSavings ?? s?.budgetSavings ?? 0) }));
      } else if (savingsRes.status === 404) {
        setSummary((prev) => ({ ...prev, totalSavings: 0 }));
      }

      // fetch top buyers (procuring entities)
      const buyersRes = await fetch(`/api/analytics/top/procuring-entities?top=5`);
      if (buyersRes.ok) {
        const buyers = await buyersRes.json();
        // NameValueDto -> { Name, Total }
        const mapped = (buyers || []).map((b) => ({ name: b.Name || b.name, amount: Number(b.Total ?? b.total ?? 0) }));
        setSummary((prev) => ({ ...prev, topBuyers: mapped }));
      }

      // fetch top suppliers
      const suppliersRes = await fetch(`/api/analytics/top/suppliers?top=5`);
      if (suppliersRes.ok) {
        const suppliers = await suppliersRes.json();
        const mapped = (suppliers || []).map((s) => ({ name: s.Name || s.name, amount: Number(s.Total ?? s.total ?? 0) }));
        setSummary((prev) => ({ ...prev, topSuppliers: mapped }));
      }
    } catch (e) {
      setMessage("Error loading analytics; showing mock data.");
      setSummary(mockData(externalId));
    }
    setLoading(false);
  }

  function mockData(id) {
    return {
      totalSavings: Math.floor(Math.random() * 500000) + 10000,
      topBuyers: Array.from({ length: 5 }, (_, i) => ({ name: `Buyer ${i + 1} (${id})`, amount: Math.floor(Math.random() * 100000) })),
      topSuppliers: Array.from({ length: 5 }, (_, i) => ({ name: `Supplier ${i + 1} (${id})`, amount: Math.floor(Math.random() * 100000) }))
    };
  }

  async function refreshData() {
    setMessage("Refreshing...");
    try {
      const res = await fetch(`/api/DataSync/refresh`, { method: "POST" });
      if (!res.ok) throw new Error("Refresh failed");
      setMessage("Data refreshed successfully.");
    } catch (e) {
      setMessage("Refresh endpoint not available;");
    }
  }

  return (
    <div className="container">
      <h1>Prozorro Dashboard</h1>

      <div className="controls">
        <label>
          Select tender:
          <select value={selected} onChange={(e) => setSelected(e.target.value)}>
            {tenders.map((t) => (
              <option key={t.externalId} value={t.externalId}>
                {t.name || t.externalId}
              </option>
            ))}
          </select>
        </label>

        <button onClick={refreshData}>Refresh Data</button>
      </div>

      <div className="widget">
        <div className="big-number">{loading ? "…" : formatMoney(summary.totalSavings)}</div>
        <div className="label">Total savings</div>
      </div>

      <div className="tables">
        <div className="table">
          <h3>Top 5 Buyers</h3>
          <table>
            <thead>
              <tr><th>Buyer</th><th>Amount</th></tr>
            </thead>
            <tbody>
              {summary.topBuyers.map((b, i) => (
                <tr key={i}><td>{b.name}</td><td>{formatMoney(b.amount)}</td></tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="table">
          <h3>Top 5 Suppliers</h3>
          <table>
            <thead>
              <tr><th>Supplier</th><th>Amount</th></tr>
            </thead>
            <tbody>
              {summary.topSuppliers.map((s, i) => (
                <tr key={i}><td>{s.name}</td><td>{formatMoney(s.amount)}</td></tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="status">{message}</div>
    </div>
  );
}
