import React, { useEffect, useState } from "react";

function formatMoney(n) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", maximumFractionDigits: 0 }).format(n);
}

export default function App() {
  const [ids] = useState(["id-1", "id-2", "id-3"]);
  const [selected, setSelected] = useState(ids[0]);
  const [summary, setSummary] = useState({ totalSavings: 0, topBuyers: [], topSuppliers: [] });
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");

  useEffect(() => {
    fetchSummary(selected);
  }, [selected]);

  async function fetchSummary(id) {
    setLoading(true);
    setMessage("");
    try {
      // Replace endpoint with your backend API. For now this tries to call /api/dashboard/summary?id=...
      const res = await fetch(`/api/dashboard/summary?id=${encodeURIComponent(id)}`);
      if (!res.ok) {
        // Fallback to mock data
        setSummary(mockData(id));
      } else {
        const data = await res.json();
        setSummary(data);
      }
    } catch (e) {
      setSummary(mockData(id));
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
      const res = await fetch(`/api/import`, { method: "POST" });
      if (!res.ok) throw new Error("Import failed");
      setMessage("Import triggered successfully.");
    } catch (e) {
      setMessage("Import endpoint not available; try wiring /api/import on backend.");
    }
  }

  return (
    <div className="container">
      <h1>Prozorro Dashboard</h1>

      <div className="controls">
        <label>
          Select id:
          <select value={selected} onChange={(e) => setSelected(e.target.value)}>
            {ids.map((id) => (
              <option key={id} value={id}>
                {id}
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
