# ☕ Coffee Ocelot Notification System

This project is a **microservices demo** that showcases event-driven communication between services using **.NET 8**, **React (Vite)**, and **Redpanda (Kafka-compatible streaming platform)**.  
It demonstrates how orders can be placed via an API, published as Kafka events, and consumed by another service to notify clients.

---

## 📖 Overview

- **Orders Service (C# .NET 9)**  
  Accepts order requests (e.g., coffee orders), saves them, and publishes order events to Kafka (`orders` topic).

- **Notification Service (C# .NET 9)**  
  Listens to Kafka `orders` topic, stores the last 20 events in memory, and exposes them via REST for the frontend.

- **Frontend (React + Vite)**  
  A web UI to place new orders and view live notifications.

- **Redpanda**  
  Lightweight Kafka replacement used for local development without Zookeeper.

- **Ocelot API Gateway (optional)**  
  Provides a single entry point to route traffic between services, apply rate limiting, and manage authentication.

---

## 🛠️ Core Features

✅ Place new orders via the Orders API or frontend  
✅ Orders are published as events to Kafka (`orders` topic)  
✅ Notification Service consumes and stores the latest events  
✅ React frontend displays events in real time  
✅ Docker Compose setup for easy local development  
✅ CORS enabled for React–API communication  

---
📚 Tech Stack

Backend: C# .NET 8 Web API

Frontend: React + Vite + Tailwind (optional)

Event Streaming: Redpanda (Kafka API)

Gateway: Ocelot

Containerization: Docker & Docker Compose

---
⚙️ Configuration

Environment variables defined in docker-compose.yml:

ASPNETCORE_URLS=http://+:<port>

KAFKA_BROKER=redpanda:9092

Ocelot routes defined in ocelot.json:

/api/catalog/*

/api/orders/*

---
🌟 Future Enhancements

Here are possible improvements for the next iterations:

🔐 Authentication & Authorization → Secure APIs with JWT

🗃 Persistent Storage → Save orders & notifications in a database (SQL/NoSQL)

📡 Real-time WebSockets / SignalR → Push notifications to clients instantly

📊 Dashboard → Analytics for order volume, revenue, and customer activity

🧪 Unit & Integration Tests → Automated tests for reliability

☁️ Cloud Deployment → Deploy with Kubernetes on Azure/AWS/GCP