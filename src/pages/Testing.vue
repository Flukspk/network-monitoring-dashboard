<template>
  <div class="testing-page">
    <!-- Header Section -->
    <header class="page-header">
      <div class="header-content">
        <div class="header-text">
          <p class="eyebrow">Network Diagnostics</p>
          <h1>Live Agent Monitor</h1>
          <p class="subtitle">
            Real-time latency monitoring and network diagnostics
          </p>
        </div>
        <div class="header-actions">
          <button
            v-if="!isMonitoring"
            class="btn-primary btn-icon"
            @click="openModal"
          >
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path
                d="M8 3v10M3 8h10"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
              />
            </svg>
            New Test
          </button>
          <button v-else class="btn-danger btn-icon" @click="stopMonitoring">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <rect
                x="4"
                y="4"
                width="8"
                height="8"
                rx="1"
                fill="currentColor"
              />
            </svg>
            Stop Monitoring
          </button>
        </div>
      </div>
    </header>

    <!-- Dashboard View (When Not Monitoring) -->
    <section v-if="!isMonitoring" class="dashboard-section">
      <div class="cards-grid">
        <!-- Last Activity Card -->
        <div class="card activity-card">
          <div class="card-header">
            <div class="card-title-group">
              <h3>Last Activity</h3>
              <button class="btn-icon-sm" @click="fetchLatest" title="Refresh">
                <svg width="14" height="14" viewBox="0 0 16 16" fill="none">
                  <path
                    d="M8 2v4M8 10v4M2 8h4M10 8h4"
                    stroke="currentColor"
                    stroke-width="1.5"
                    stroke-linecap="round"
                  />
                  <path
                    d="M2 8a6 6 0 0 1 6-6M14 8a6 6 0 0 1-6 6"
                    stroke="currentColor"
                    stroke-width="1.5"
                    stroke-linecap="round"
                  />
                </svg>
              </button>
            </div>
          </div>

          <div v-if="latestMetric" class="card-body">
            <div class="metric-item">
              <span class="metric-label">Target</span>
              <span class="metric-value highlight">{{
                latestMetric.target
              }}</span>
            </div>
            <div class="metric-item">
              <span class="metric-label">Type</span>
              <span class="metric-badge">{{ latestMetric.metricType }}</span>
            </div>
            <div class="metric-item">
              <span class="metric-label">Status</span>
              <span
                class="status-badge"
                :class="getStatusClass(latestMetric.status)"
              >
                <span class="status-dot"></span>
                {{ latestMetric.status }}
              </span>
            </div>
            <div class="metric-item">
              <span class="metric-label">Latency</span>
              <span class="metric-value large"
                >{{ latestMetric.value }}<span class="unit">ms</span></span
              >
            </div>
            <div class="metric-item">
              <span class="metric-label">Timestamp</span>
              <span class="metric-value dimmed">{{
                formatTime(latestMetric.timestamp)
              }}</span>
            </div>
          </div>

          <div v-else class="empty-state">
            <svg
              width="48"
              height="48"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="1.5"
            >
              <circle cx="12" cy="12" r="10" />
              <path d="M12 8v4M12 16h.01" />
            </svg>
            <p>No recent activity</p>
            <p class="empty-hint">Start a new test to see results here</p>
          </div>
        </div>

        <!-- Quick Start Card -->
        <div class="card info-card">
          <div class="card-header">
            <h3>Quick Start</h3>
          </div>
          <div class="card-body">
            <ol class="steps-list">
              <li>
                <span class="step-number">1</span>
                <div>
                  <strong>Click "New Test"</strong>
                  <p>Open the test configuration dialog</p>
                </div>
              </li>
              <li>
                <span class="step-number">2</span>
                <div>
                  <strong>Select Agent & Target</strong>
                  <p>Choose test type and enter target address</p>
                </div>
              </li>
              <li>
                <span class="step-number">3</span>
                <div>
                  <strong>Start Monitoring</strong>
                  <p>View real-time graph and logs</p>
                </div>
              </li>
            </ol>
          </div>
        </div>
      </div>
    </section>

    <!-- Monitoring View (When Active) -->
    <section v-else class="monitoring-section">
      <!-- Live Stats Bar -->
      <div class="stats-bar">
        <div class="stat-item">
          <span class="stat-label">Target</span>
          <span class="stat-value">{{ targetUrl }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">Agent</span>
          <span class="stat-value">{{ selectedAgentMode }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">Status</span>
          <span class="live-badge">
            <span class="pulse-dot"></span>
            Live
          </span>
        </div>
        <div class="stat-item">
          <span class="stat-label">Updates</span>
          <span class="stat-value">{{ logs.length }} tests</span>
        </div>
      </div>

      <!-- Graph Panel -->
      <div class="card graph-card">
        <div class="card-header">
          <div class="card-title-group">
            <h3>Latency Graph</h3>
            <div class="graph-controls">
              <span class="graph-range">Min: {{ minVal }}ms</span>
              <span class="graph-range">Max: {{ maxVal }}ms</span>
            </div>
          </div>
        </div>
        <div class="graph-container">
          <canvas ref="chartCanvas" width="800" height="300"></canvas>
        </div>
      </div>

      <!-- Recent Logs Table -->
      <div class="card logs-card">
        <div class="card-header">
          <h3>Recent Logs</h3>
          <span class="logs-count">{{ logs.length }} entries</span>
        </div>
        <div class="table-container">
          <table class="logs-table">
            <thead>
              <tr>
                <th>Time</th>
                <th>Latency</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="logs.length === 0">
                <td colspan="3" class="empty-table">
                  <span>Waiting for test results...</span>
                </td>
              </tr>
              <tr v-for="(log, i) in logs" :key="i" class="log-row">
                <td class="log-time">{{ log.time }}</td>
                <td class="log-value">
                  <span class="value-number">{{ log.value }}</span>
                  <span class="value-unit">ms</span>
                </td>
                <td>
                  <span
                    class="status-badge"
                    :class="getStatusClass(log.status)"
                  >
                    <span class="status-dot"></span>
                    {{ log.status }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </section>

    <!-- Modal Dialog -->
    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal-dialog">
        <div class="modal-header">
          <h2>Configure Test</h2>
          <button class="close-btn" @click="closeModal" aria-label="Close">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
              <path
                d="M5 5l10 10M15 5l-10 10"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
              />
            </svg>
          </button>
        </div>

        <div class="modal-body">
          <div class="form-group">
            <label for="agent-select">
              <span>Agent Type</span>
              <span class="label-hint">Select the type of network test</span>
            </label>
            <select
              id="agent-select"
              v-model="selectedAgentMode"
              class="form-select"
            >
              <option value="PING">Agent 1 - Ping (ICMP)</option>
              <option value="HTTP">Agent 2 - HTTP Request</option>
              <option value="TRACEROUTE">Agent 3 - Traceroute</option>
            </select>
          </div>

          <div class="form-group">
            <label for="target-input">
              <span>Target</span>
              <span class="label-hint">IP address, domain, or URL</span>
            </label>
            <input
              id="target-input"
              v-model="targetUrl"
              type="text"
              class="form-input"
              placeholder="e.g. 8.8.8.8, google.com, https://www.github.com"
              @keyup.enter="startTestFromModal"
              autofocus
            />

            <!-- Quick Examples -->
            <div class="examples-section">
              <p class="examples-label">Quick examples:</p>
              <div class="examples-grid">
                <template v-if="selectedAgentMode === 'PING'">
                  <button
                    v-for="target in pingExamples"
                    :key="target"
                    class="example-chip"
                    @click="targetUrl = target"
                  >
                    {{ target }}
                  </button>
                </template>
                <template v-else-if="selectedAgentMode === 'HTTP'">
                  <button
                    v-for="target in httpExamples"
                    :key="target"
                    class="example-chip"
                    @click="targetUrl = target"
                  >
                    {{ target.replace("https://", "").replace("www.", "") }}
                  </button>
                </template>
                <template v-else>
                  <button
                    v-for="target in traceExamples"
                    :key="target"
                    class="example-chip"
                    @click="targetUrl = target"
                  >
                    {{ target }}
                  </button>
                </template>
              </div>
            </div>
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn-secondary" @click="closeModal">Cancel</button>
          <button
            class="btn-primary"
            @click="startTestFromModal"
            :disabled="!targetUrl.trim() || isRunningTest"
          >
            <span v-if="isRunningTest">Starting...</span>
            <span v-else>Start Monitoring</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, nextTick } from "vue";
import axios from "axios";

// State
const isMonitoring = ref(false);
const showModal = ref(false);
const latestMetric = ref(null);
const selectedAgentMode = ref("PING");
const targetUrl = ref("");
const logs = ref([]);
const dataPoints = ref(Array.from({ length: 20 }, () => 0));
const timer = ref(null);
const chartCanvas = ref(null);
const maxVal = ref(100);
const minVal = ref(0);
const isRunningTest = ref(false);

// Example targets
const pingExamples = [
  "1.1.1.1",
  "8.8.8.8",
  "8.8.4.4",
  "208.67.222.222",
  "9.9.9.9",
];
const httpExamples = [
  "https://www.google.com",
  "https://www.github.com",
  "https://www.cloudflare.com",
  "https://httpbin.org/get",
  "https://www.example.com",
];
const traceExamples = ["8.8.8.8", "1.1.1.1", "google.com"];

// API Configuration
const getApiBaseUrl = () => {
  if (import.meta.env.VITE_API_URL) return import.meta.env.VITE_API_URL;
  if (
    window.location.port === "8080" ||
    window.location.hostname !== "localhost"
  )
    return "/api";
  return "http://localhost:5050";
};
const API_BASE_URL = getApiBaseUrl();

// Helper Functions
const formatTime = (timestamp) => {
  return new Date(timestamp).toLocaleString();
};

const getStatusClass = (status) => {
  if (status === "Success") return "status-success";
  if (status === "Investigate" || status === "Failed" || status === "Error")
    return "status-danger";
  return "status-warning";
};

// Modal Actions
const openModal = () => {
  targetUrl.value = "";
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

// Backend API Calls
const toggleBackendMonitoring = async (action) => {
  try {
    const endpoint = API_BASE_URL.startsWith("/")
      ? `${API_BASE_URL}/metrics/${action}`
      : `${API_BASE_URL}/api/metrics/${action}`;
    await axios.post(endpoint, {
      Target: targetUrl.value,
      Type: selectedAgentMode.value,
    });
  } catch (err) {
    console.error(`Failed to ${action} backend:`, err);
  }
};

// Start Monitoring
const startTestFromModal = async () => {
  if (!targetUrl.value.trim()) {
    alert("Please enter a target");
    return;
  }

  showModal.value = false;
  isMonitoring.value = true;
  isRunningTest.value = true;

  // Reset data
  logs.value = [];
  dataPoints.value = Array.from({ length: 20 }, () => 0);

  // Run test immediately, then every 5 seconds
  runTestAndUpdate();
  timer.value = setInterval(runTestAndUpdate, 5000); // Every 5 seconds
  isRunningTest.value = false;
};

// Run test and update display
const runTestAndUpdate = async () => {
  if (!targetUrl.value.trim()) {
    console.warn("No target specified, skipping test");
    return;
  }

  try {
    const apiEndpoint = API_BASE_URL.startsWith("/")
      ? `${API_BASE_URL}/agent/run`
      : `${API_BASE_URL}/api/agent/run`;

    await axios.post(apiEndpoint, {
      AgentId: selectedAgentMode.value,
      Target: targetUrl.value,
      MetricType: selectedAgentMode.value,
    });

    // Wait a moment for database, then fetch and update
    setTimeout(() => {
      fetchData();
    }, 800);
  } catch (err) {
    console.error("Test error:", err);
    // Still try to fetch data in case previous tests succeeded
    setTimeout(() => {
      fetchData();
    }, 800);
  }
};

// Stop Monitoring
const stopMonitoring = async () => {
  isMonitoring.value = false;
  if (timer.value) {
    clearInterval(timer.value);
    timer.value = null;
  }
  fetchLatest();
};

// Fetch Latest Activity
const fetchLatest = async () => {
  try {
    const apiEndpoint = API_BASE_URL.startsWith("/")
      ? `${API_BASE_URL}/metrics/filter`
      : `${API_BASE_URL}/api/metrics/filter`;
    const res = await axios.get(apiEndpoint);
    if (res.data && res.data.length > 0) {
      latestMetric.value = res.data[0];
    }
  } catch (err) {
    console.error("Fetch latest error:", err);
  }
};

// Fetch Data During Monitoring
const fetchData = async () => {
  try {
    const apiEndpoint = API_BASE_URL.startsWith("/")
      ? `${API_BASE_URL}/metrics/filter?target=${encodeURIComponent(
          targetUrl.value
        )}&type=${selectedAgentMode.value}`
      : `${API_BASE_URL}/api/metrics/filter?target=${encodeURIComponent(
          targetUrl.value
        )}&type=${selectedAgentMode.value}`;

    const res = await axios.get(apiEndpoint);

    if (res.data && res.data.length > 0) {
      const latestData = res.data[0];
      const timeStr = new Date(latestData.timestamp).toLocaleTimeString();

      if (
        logs.value.length === 0 ||
        logs.value[0].timestamp !== latestData.timestamp
      ) {
        logs.value.unshift({
          time: timeStr,
          value: latestData.value,
          status: latestData.status,
          timestamp: latestData.timestamp,
        });

        // Keep only last 50 logs
        if (logs.value.length > 50) {
          logs.value = logs.value.slice(0, 50);
        }

        dataPoints.value.shift();
        dataPoints.value.push(latestData.value);

        await nextTick();
        drawGraph();
      }
    }
  } catch (err) {
    console.error("Fetch error:", err);
  }
};

// Draw Graph
const drawGraph = () => {
  const canvas = chartCanvas.value;
  if (!canvas) return;
  const ctx = canvas.getContext("2d");
  const width = canvas.width;
  const height = canvas.height;
  const data = dataPoints.value;

  ctx.clearRect(0, 0, width, height);

  const max = Math.max(...data, 50) * 1.2;
  maxVal.value = Math.round(max);
  const min = Math.min(...data.filter((d) => d > 0), 0);
  minVal.value = Math.round(min);

  // Draw grid
  ctx.strokeStyle = "rgba(255, 255, 255, 0.05)";
  ctx.lineWidth = 1;
  for (let i = 0; i <= 5; i++) {
    const y = (height / 5) * i;
    ctx.beginPath();
    ctx.moveTo(0, y);
    ctx.lineTo(width, y);
    ctx.stroke();
  }

  // Draw line
  ctx.beginPath();
  ctx.strokeStyle = "#4da5dd";
  ctx.lineWidth = 3;
  ctx.lineCap = "round";
  ctx.lineJoin = "round";

  const stepX = width / (data.length - 1);
  data.forEach((val, index) => {
    const x = index * stepX;
    const y = height - (val / max) * height;
    if (index === 0) ctx.moveTo(x, y);
    else ctx.lineTo(x, y);
  });
  ctx.stroke();

  // Draw area fill
  ctx.lineTo(width, height);
  ctx.lineTo(0, height);
  ctx.closePath();
  const gradient = ctx.createLinearGradient(0, 0, 0, height);
  gradient.addColorStop(0, "rgba(77, 165, 221, 0.2)");
  gradient.addColorStop(1, "rgba(77, 165, 221, 0.01)");
  ctx.fillStyle = gradient;
  ctx.fill();

  // Draw data points
  ctx.fillStyle = "#4da5dd";
  data.forEach((val, index) => {
    if (val > 0) {
      const x = index * stepX;
      const y = height - (val / max) * height;
      ctx.beginPath();
      ctx.arc(x, y, 3, 0, Math.PI * 2);
      ctx.fill();
    }
  });
};

onMounted(() => {
  fetchLatest();
});

onUnmounted(() => {
  if (timer.value) clearInterval(timer.value);
});
</script>

<style scoped>
/* Page Layout */
.testing-page {
  min-height: 100vh;
  padding: 40px clamp(24px, 4vw, 60px);
  background: #050608;
  color: white;
}

/* Header */
.page-header {
  margin-bottom: 32px;
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 24px;
}

.header-text {
  flex: 1;
}

.eyebrow {
  text-transform: uppercase;
  letter-spacing: 0.2em;
  font-size: 12px;
  color: #8b949e;
  margin-bottom: 8px;
}

.page-header h1 {
  font-size: clamp(28px, 4vw, 36px);
  font-weight: 700;
  margin: 0 0 8px 0;
  color: white;
}

.subtitle {
  font-size: 15px;
  color: #8b949e;
  margin: 0;
}

/* Buttons */
.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 12px 24px;
  background: #238636;
  color: white;
  border: none;
  border-radius: 6px;
  font-weight: 600;
  font-size: 15px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-primary:hover:not(:disabled) {
  background: #2ea043;
  transform: translateY(-1px);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.btn-danger {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 12px 24px;
  background: #da3633;
  color: white;
  border: none;
  border-radius: 6px;
  font-weight: 600;
  font-size: 15px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-danger:hover {
  background: #f85149;
  transform: translateY(-1px);
}

.btn-secondary {
  padding: 10px 20px;
  background: transparent;
  border: 1px solid #30363d;
  color: white;
  border-radius: 6px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-secondary:hover {
  border-color: #8b949e;
  background: rgba(255, 255, 255, 0.05);
}

.btn-icon-sm {
  background: transparent;
  border: none;
  color: #8b949e;
  cursor: pointer;
  padding: 4px;
  border-radius: 4px;
  transition: all 0.2s ease;
}

.btn-icon-sm:hover {
  color: white;
  background: rgba(255, 255, 255, 0.05);
}

/* Cards */
.cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 24px;
}

.card {
  background: linear-gradient(135deg, #161b22 0%, #1c2128 100%);
  border: 1px solid #30363d;
  border-radius: 8px;
  padding: 24px;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.3);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  padding-bottom: 16px;
  border-bottom: 1px solid #30363d;
}

.card-title-group {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.card h3 {
  font-size: 18px;
  font-weight: 600;
  margin: 0;
  color: white;
}

/* Activity Card */
.metric-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 0;
  border-bottom: 1px solid #30363d;
}

.metric-item:last-child {
  border-bottom: none;
}

.metric-label {
  font-size: 13px;
  color: #8b949e;
  font-weight: 500;
}

.metric-value {
  font-size: 15px;
  color: white;
  font-weight: 600;
}

.metric-value.highlight {
  color: #4da5dd;
  font-size: 16px;
}

.metric-value.large {
  font-size: 20px;
  font-weight: 700;
  color: white;
}

.metric-value.large .unit {
  font-size: 14px;
  font-weight: 500;
  margin-left: 4px;
  color: #8b949e;
}

.metric-value.dimmed {
  color: #8b949e;
  font-size: 13px;
  font-weight: 400;
}

.metric-badge {
  padding: 4px 12px;
  background: rgba(77, 165, 221, 0.15);
  color: #4da5dd;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

.status-badge.status-success {
  background: rgba(63, 185, 80, 0.15);
  color: #3fb950;
}

.status-badge.status-danger {
  background: rgba(248, 81, 73, 0.15);
  color: #f85149;
}

.status-badge.status-warning {
  background: rgba(210, 153, 34, 0.15);
  color: #d29922;
}

.status-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: currentColor;
}

/* Empty State */
.empty-state {
  text-align: center;
  padding: 48px 24px;
  color: #8b949e;
}

.empty-state svg {
  margin-bottom: 16px;
  opacity: 0.5;
}

.empty-state p {
  margin: 8px 0;
  font-size: 15px;
}

.empty-hint {
  font-size: 13px;
  opacity: 0.7;
}

/* Steps List */
.steps-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.steps-list li {
  display: flex;
  gap: 16px;
  margin-bottom: 24px;
  align-items: flex-start;
}

.steps-list li:last-child {
  margin-bottom: 0;
}

.step-number {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  background: #238636;
  color: white;
  border-radius: 50%;
  font-weight: 700;
  font-size: 14px;
  flex-shrink: 0;
}

.steps-list li div {
  flex: 1;
  padding-top: 4px;
}

.steps-list strong {
  display: block;
  color: white;
  margin-bottom: 4px;
  font-size: 14px;
}

.steps-list p {
  color: #8b949e;
  font-size: 13px;
  margin: 0;
}

/* Monitoring Section */
.monitoring-section {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.stats-bar {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 16px;
  padding: 20px;
  background: linear-gradient(135deg, #161b22 0%, #1c2128 100%);
  border: 1px solid #30363d;
  border-radius: 8px;
}

.stat-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.stat-label {
  font-size: 12px;
  color: #8b949e;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  font-weight: 600;
}

.stat-value {
  font-size: 16px;
  font-weight: 600;
  color: white;
}

.live-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 12px;
  background: rgba(63, 185, 80, 0.15);
  color: #3fb950;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

.pulse-dot {
  width: 8px;
  height: 8px;
  background: #3fb950;
  border-radius: 50%;
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0%,
  100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.7;
    transform: scale(1.2);
  }
}

/* Graph Card */
.graph-card {
  padding: 0;
  overflow: hidden;
}

.graph-card .card-header {
  padding: 24px 24px 0 24px;
  margin-bottom: 0;
}

.graph-controls {
  display: flex;
  gap: 16px;
  font-size: 12px;
  color: #8b949e;
}

.graph-container {
  padding: 24px;
  background: rgba(13, 17, 23, 0.5);
  border-radius: 6px;
  margin: 20px;
}

.graph-container canvas {
  width: 100%;
  height: auto;
  display: block;
}

/* Logs Card */
.logs-card .card-header {
  margin-bottom: 0;
}

.logs-count {
  font-size: 12px;
  color: #8b949e;
  font-weight: 500;
}

.table-container {
  max-height: 400px;
  overflow-y: auto;
  margin-top: 16px;
}

.logs-table {
  width: 100%;
  border-collapse: collapse;
}

.logs-table thead {
  position: sticky;
  top: 0;
  background: #161b22;
  z-index: 10;
}

.logs-table th {
  padding: 12px 16px;
  text-align: left;
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #8b949e;
  border-bottom: 2px solid #30363d;
}

.log-row {
  transition: background 0.15s ease;
}

.log-row:hover {
  background: rgba(255, 255, 255, 0.02);
}

.log-row td {
  padding: 14px 16px;
  border-bottom: 1px solid #30363d;
}

.log-time {
  font-size: 13px;
  color: #8b949e;
  font-family: "Monaco", "Menlo", monospace;
}

.log-value {
  display: flex;
  align-items: baseline;
  gap: 4px;
}

.value-number {
  font-size: 16px;
  font-weight: 600;
  color: white;
}

.value-unit {
  font-size: 12px;
  color: #8b949e;
}

.empty-table {
  text-align: center;
  padding: 48px;
  color: #8b949e;
  font-style: italic;
}

/* Modal */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.75);
  backdrop-filter: blur(8px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  animation: fadeIn 0.2s ease;
}

.modal-dialog {
  background: linear-gradient(135deg, #161b22 0%, #1c2128 100%);
  border: 1px solid #30363d;
  border-radius: 8px;
  width: 90%;
  max-width: 520px;
  max-height: 90vh;
  overflow: hidden;
  box-shadow: 0 24px 64px rgba(0, 0, 0, 0.5);
  animation: slideUp 0.3s ease;
  display: flex;
  flex-direction: column;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 24px;
  border-bottom: 1px solid #30363d;
}

.modal-header h2 {
  font-size: 20px;
  font-weight: 600;
  margin: 0;
  color: white;
}

.close-btn {
  background: transparent;
  border: none;
  color: #8b949e;
  cursor: pointer;
  padding: 4px;
  border-radius: 4px;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.close-btn:hover {
  color: white;
  background: rgba(255, 255, 255, 0.05);
}

.modal-body {
  padding: 24px;
  overflow-y: auto;
  flex: 1;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 20px 24px;
  border-top: 1px solid #30363d;
  background: rgba(13, 17, 23, 0.5);
}

/* Form Elements */
.form-group {
  margin-bottom: 24px;
}

.form-group label {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 10px;
  font-size: 14px;
  font-weight: 600;
  color: white;
}

.label-hint {
  font-size: 12px;
  font-weight: 400;
  color: #8b949e;
}

.form-input,
.form-select {
  width: 100%;
  padding: 12px 16px;
  background: rgba(13, 17, 23, 0.8);
  border: 1px solid #30363d;
  border-radius: 6px;
  color: white;
  font-size: 15px;
  transition: all 0.2s ease;
  font-family: inherit;
}

.form-input:focus,
.form-select:focus {
  outline: none;
  border-color: #4da5dd;
  box-shadow: 0 0 0 3px rgba(77, 165, 221, 0.1);
  background: rgba(13, 17, 23, 0.95);
}

.form-select {
  cursor: pointer;
}

/* Examples Section */
.examples-section {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid #30363d;
}

.examples-label {
  font-size: 12px;
  color: #8b949e;
  margin-bottom: 10px;
  display: block;
}

.examples-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.example-chip {
  padding: 6px 12px;
  background: rgba(77, 165, 221, 0.1);
  border: 1px solid rgba(77, 165, 221, 0.2);
  border-radius: 16px;
  color: #4da5dd;
  font-size: 12px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.example-chip:hover {
  background: rgba(77, 165, 221, 0.2);
  border-color: #4da5dd;
  transform: translateY(-1px);
}

/* Animations */
@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

@keyframes slideUp {
  from {
    transform: translateY(20px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

/* Responsive */
@media (max-width: 768px) {
  .testing-page {
    padding: 20px;
  }

  .header-content {
    flex-direction: column;
    align-items: stretch;
  }

  .cards-grid {
    grid-template-columns: 1fr;
  }

  .stats-bar {
    grid-template-columns: repeat(2, 1fr);
  }

  .modal-dialog {
    width: 95%;
    margin: 20px;
  }
}
</style>
