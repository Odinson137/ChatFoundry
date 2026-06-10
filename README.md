<h1 align="center">
  <br>
  💬 ChatFoundry
  <br>
</h1>

<h4 align="center">Enterprise-Grade No-Code Chatbot Builder & Workflow Automation Platform</h4>

<p align="center">
  <a href="#about-the-project">About</a> •
  <a href="#key-features">Key Features</a> •
  <a href="#architecture">Architecture</a> •
  <a href="#tech-stack">Tech Stack</a> •
  <a href="#getting-started">Getting Started</a> •
  <a href="#services-overview">Services</a>
</p>

---

> [!TIP]
> **Live Demo:** You can access the deployed application at **[https://chatfoundry.dpdns.org](https://chatfoundry.dpdns.org)**.

## 🚀 About the Project

**ChatFoundry** is an advanced, self-hosted or cloud-ready no-code platform for creating, managing, and analyzing chatbots. It provides a visual drag-and-drop workflow designer, allowing users to build complex conversational logic without writing a single line of code.

Designed with a robust microservice architecture, ChatFoundry scales effortlessly and supports multi-tenant SaaS environments out-of-the-box, making it ideal for enterprises, agencies, and CRM integrations.

## ✨ Key Features

* **🎨 Visual Workflow Designer:** Build chatbots using a seamless drag-and-drop Blazor WebAssembly interface.
* **🧠 AI-Powered Nodes:** Native integration with LLM providers (OpenAI, GLM, with a failover system) directly inside your workflows via the `AIGenerate` node.
* **⚡ Event-Driven Logic:** Advanced node conditions (Regex, Contains, StartsWith, InList) to handle complex routing and data parsing.
* **👥 Live Operator Chat:** Built-in Live Chat interface for human fallback (`TransferToOperator`). Operators can take over chatbot conversations seamlessly.
* **📈 Built-in Analytics & Session Replay:** Inspect exactly how a user traversed the workflow graph step-by-step with the Session Replay feature.
* **🏢 Multi-Tenancy & Billing:** Built-in workspace separation (`CompanyService`) and flexible subscription plans (with support for manual invoicing).
* **🔒 Enterprise Compliance:** Database-per-service architecture allows strict data residency compliance (like 152-FZ in the CIS region).

### 📸 Interface Previews

<!-- Place your workflow editor screenshot here -->
> 🖼 **Visual Workflow Designer:**  
> <img width="1381" height="778" alt="изображение" src="https://github.com/user-attachments/assets/739d99b4-2b2e-406f-b640-3001003026b0" />

<!-- Place your payment settings & balance screenshot here -->
> 🖼 **Billing & Payment Settings:**  
> <img width="1386" height="778" alt="изображение" src="https://github.com/user-attachments/assets/663d531e-c6dd-47d6-a24c-348d7b79cb35" />

<!-- Place your live chat screenshot here -->
> 🖼 **Live Operator Chat:**  
> <img width="1386" height="773" alt="изображение" src="https://github.com/user-attachments/assets/42152ec0-5e98-4d96-9f3b-7fdd02649e9d" />

<!-- Place your client card screenshot here -->
> 🖼 **CRM Client Card:**  
> <img width="1385" height="774" alt="изображение" src="https://github.com/user-attachments/assets/5d10e1cc-e114-4bbd-8f48-f5b66a730413" />

<!-- Place your sessions replay screenshot here -->
> 🖼 **Session Replay & Analytics:**  
> <img width="1384" height="766" alt="изображение" src="https://github.com/user-attachments/assets/a99d5e72-1fec-4ddf-b176-056378fef40c" />

## 🏗 Architecture

ChatFoundry is built on a modern **Event-Driven Microservices** topology.

* **API Gateway:** Central entry point using GraphQL (HotChocolate) for the client application.
* **Synchronous Communication:** Services communicate with each other internally via **gRPC** (HTTP/2).
* **Asynchronous Communication:** **Apache Kafka** (via MassTransit Rider) handles distributed messaging, workflow execution events, and webhook processing.
* **Data Isolation:** A strict **Database-per-Service** pattern is used (8+ independent PostgreSQL 16 databases).
* **Observability:** Built-in telemetry stack using OpenTelemetry (Jaeger), Seq (Logging), and Prometheus/Grafana (Metrics).

## 🛠 Tech Stack

**Frontend:**
* [ASP.NET Core Blazor WebAssembly](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)
* Custom CSS Design System (Inter font, Glassmorphism, Fluid Animations)

**Backend:**
* C# / .NET 10.0
* [HotChocolate](https://chillicream.com/docs/hotchocolate) (GraphQL Server)
* [MassTransit](https://masstransit.io/) (Message Bus & Kafka Rider)
* gRPC (Inter-service RPC)
* Entity Framework Core 10 (Code-first approach)

**Infrastructure & Databases:**
* [PostgreSQL 16](https://www.postgresql.org/) (Primary Data Stores)
* [Redis](https://redis.io/) (Distributed Caching)
* [Apache Kafka](https://kafka.apache.org/) & Zookeeper (Event Streaming)
* Docker & Docker Compose

## 📦 Services Overview

| Service | Port (HTTP/gRPC) | Description |
|---------|-----------------|-------------|
| **Gateway** | `5000` | Entry point, routing, and GraphQL aggregation. |
| **WorkflowService** | `5010 / 5011` | Core bot engine. Evaluates graph nodes, runs AI, executes actions. |
| **IdentityServer** | `5020 / 5021` | Handles authentication, JWT issuance, and users. |
| **ClientService** | `5030 / 5031` | CRM component. Stores bot subscribers, attributes, and tags. |
| **TelegramService** | `5040` | Webhook receiver and message sender for Telegram channel. |
| **CompanyService** | `5050 / 5051` | Tenant (workspace) management and team invitations. |
| **BillingService** | `5060 / 5061` | Subscription tiers, usage quotas, and payment processing. |
| **FileService** | `5070 / 5071` | S3-compatible media upload and attachment processing. |
| **NotificationService** | `5080` | Internal application notifications. |
| **SchedulerService** | `5090 / 5091` | Distributed cron jobs and delayed workflow actions (TimeStart/Wait nodes). |
| **FreeLlmApiService** | `3001` | OpenAI-compatible endpoint aggregating multiple free LLM providers with automatic routing and fallback mechanisms. |

## 🚀 Getting Started (Local Development)

### Prerequisites
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* IDE (JetBrains Rider or Visual Studio)

### Run Infrastructure

To start the entire microservice cluster locally:

#### Standard Cluster (8 PostgreSQL instances + Kafka + Observability)
```bash
cd deployment
docker-compose up -d
```

#### Consolidated Lite Cluster (1 PostgreSQL instance - highly recommended for local development/low-resource hosting)
```bash
cd deployment
docker-compose -f docker-compose.lite.yml up -d
```
This consolidated option runs all 8 service databases in a single Postgres container, saving several gigabytes of RAM.

Once the containers are running, you can access the web application directly at **`http://localhost:7555`** (no local .NET installation required!).

### 🧠 Using Local LLMs (Ollama)
You can connect ChatFoundry to a local Ollama instance without requiring an API key. 
In `services/WorkflowService/src/WorkflowService/appsettings.json`, add your local Ollama configuration under `LlmProviders:OpenAiCompatible`:
```json
{
  "Name": "Ollama",
  "ApiKey": "",
  "ApiUrl": "http://host.docker.internal:11434/v1/chat/completions",
  "Model": "llama3"
}
```
*(Use `http://host.docker.internal:11434` when running services inside Docker so they can route to host services).*

### 🛠 Local Development (Alternative to Docker Client)
If you want to run the frontend client with hot reload for development instead of using the Docker container:
1. Stop the Docker client container: `docker-compose stop web-client`
2. Navigate to the Blazor Client project and start the development server:

```bash
cd webApps/src/BlazorClient
dotnet watch run
```

---

## 📜 Licensing
ChatFoundry is fully open-source and released under the **Apache License 2.0**. You are free to run it self-hosted, modify, and distribute it for private or commercial use.

---
<p align="center">
  <i>Built with ❤️ for modern conversation automation.</i>
</p>
