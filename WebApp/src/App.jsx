import { useEffect, useState } from "react";

const API = "http://localhost:7000";     // Gateway
const EVENTS_API = "http://localhost:7000/api/events"; // NotificationService

export default function App() {
  const [token, setToken] = useState(null);
  const [items, setItems] = useState([]);
  const [orders, setOrders] = useState([]);
  const [events, setEvents] = useState([]); // Kafka events
  const [selected, setSelected] = useState(null);
  const [qty, setQty] = useState(1);
  const [loading, setLoading] = useState(false);

  const authHeaders = token ? { Authorization: `Bearer ${token}` } : {};

  const safeJson = async (res) => {
    if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
    return res.json();
  };

  // 1) Get JWT token from Gateway
  useEffect(() => {
    fetch(`${API}/dev/token`, { method: "POST" })
      .then(safeJson)
      .then((d) => setToken(d.access_token))
      .catch((err) => console.error("get token failed:", err));
  }, []);

  // 2) Fetch catalog + orders once token is available
  useEffect(() => {
    if (!token) return;
    fetch(`${API}/api/catalog/items`, { headers: authHeaders })
      .then(safeJson)
      .then(setItems)
      .catch((err) => console.error("catalog failed:", err));

    fetch(`${API}/api/orders`, { headers: authHeaders })
      .then(safeJson)
      .then(setOrders)
      .catch((err) => console.error("orders failed:", err));
  }, [token]);

  // 3) Poll NotificationService every 3s for events
  useEffect(() => {
    const interval = setInterval(() => {
      fetch(EVENTS_API)
        .then((r) => (r.ok ? r.json() : []))
        .then(setEvents)
        .catch(() => {});
    }, 3000);
    return () => clearInterval(interval);
  }, []);

  // Place a new order
  const placeOrder = async () => {
    if (!selected) return;
    setLoading(true);
    try {
      const total = Number((selected.price * qty).toFixed(2));
      const res = await fetch(`${API}/api/orders`, {
        method: "POST",
        headers: { "Content-Type": "application/json", ...authHeaders },
        body: JSON.stringify({ itemName: selected.name, quantity: qty, total }),
      });
      const created = await safeJson(res);
      setOrders((o) => [created, ...o]);
    } catch (e) {
      console.error("placeOrder failed:", e);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ margin: 24, fontFamily: "ui-sans-serif, system-ui" }}>
      <h1>
        Coffee Store ☕ <span style={{ fontSize: "0.7em", color: "#888" }}>
          (MVP with Kafka)
        </span>
      </h1>

      {/* Catalog */}
      <section style={{ marginBottom: 24 }}>
        <h2>Catalog</h2>
        <ul>
          {items.map((i) => (
            <li key={i.id}>
              <button onClick={() => setSelected(i)} style={{ marginRight: 8 }}>
                Select
              </button>
              {i.name} — ${i.price}
            </li>
          ))}
        </ul>
      </section>

      {/* Create Order */}
      <section style={{ marginBottom: 24 }}>
        <h2>Create Order</h2>
        <div>
          <div>
            Selected:{" "}
            {selected ? `${selected.name} ($${selected.price})` : "none"}
          </div>
          <input
            type="number"
            min="1"
            value={qty}
            onChange={(e) => setQty(Number(e.target.value))}
            style={{ width: 80, marginRight: 8 }}
          />
          <button disabled={!selected || loading} onClick={placeOrder}>
            {loading ? "Placing..." : "Place order"}
          </button>
        </div>
      </section>

      {/* Orders */}
      <section style={{ marginBottom: 24 }}>
        <h2>Orders</h2>
        <ol>
          {orders.map((o) => (
            <li key={o.id}>
              #{o.id} • {o.itemName} × {o.quantity} • ${o.total} •{" "}
              {new Date(o.createdUtc).toLocaleString()}
            </li>
          ))}
        </ol>
      </section>

      {/* Kafka Events */}
      <section>
        <h2>Kafka Events</h2>
        {events.length === 0 && <p>No events yet...</p>}
        <ul>
          {events.map((e, idx) => (
            <li key={idx}>{e}</li>
          ))}
        </ul>
      </section>
    </div>
  );
}
