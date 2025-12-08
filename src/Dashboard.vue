<template>
  <main class="workspace">
    <header class="workspace-header">
      <div class="header-info">
        <p class="eyebrow">Senior project</p>
        <div class="title-row">
          <h1>Network Monitoring Dashboard</h1>
          <span class="pill header-pill">
            <span class="status-dot status-success"></span>
            Live
          </span>
        </div>
        <p class="text-muted">
          Real-time overview of latency, uptime, and target health.
        </p>
      </div>
      <div class="header-actions">
        <button class="btn-primary" @click="fetchDashboardData">Refresh Data</button>
      </div>
    </header>

    <section class="metrics-grid">
      <article v-for="metric in dashboardMetrics" :key="metric.label" class="metric-card panel">
        <div class="metric-head">
          <p class="metric-label">{{ metric.label }}</p>
          <span class="trend-pill" :class="metric.trendClass">
            {{ metric.delta }}
          </span>
        </div>
        <h2>{{ metric.value }}</h2>
        <p class="metric-subtitle">{{ metric.caption }}</p>
        <div class="progress-track">
          <div class="progress-fill" :style="{ width: metric.progress + '%' }"></div>
        </div>
      </article>
    </section>

    <section class="panels-grid">
      <article class="panel wide-panel">
        <div class="panel-header">
          <div>
            <p class="panel-title">Target Status</p>
            <p class="panel-subtitle">Current status of monitored endpoints</p>
          </div>
        </div>
        <div v-if="targetHealth.length === 0" class="no-data">No data available</div>
        <div class="probe-grid">
          <div v-for="target in targetHealth" :key="target.target" class="probe-card">
            <div class="probe-head">
              <span class="probe-name">{{ target.target }}</span>
              <span class="pill soft-pill">
                <span class="status-dot" :class="target.statusClass"></span>
                {{ target.status }}
              </span>
            </div>
            <p class="probe-meta">{{ target.metricType }}</p>
            <div class="probe-metrics">
              <div>
                <p class="probe-label">Latency</p>
                <p class="probe-value">{{ target.value }} ms</p>
              </div>
              <div>
                <p class="probe-label">Loss</p>
                <p class="probe-value">{{ target.packetLoss }}%</p>
              </div>
              <div>
                <p class="probe-label">Time</p>
                <p class="probe-value">{{ new Date(target.timestamp).toLocaleTimeString() }}</p>
              </div>
            </div>
          </div>
        </div>
      </article>

      <article class="panel table-panel">
        <div class="panel-header">
          <div>
            <p class="panel-title">High Latency Targets</p>
            <p class="panel-subtitle">Sorted by response time</p>
          </div>
        </div>
        <div v-if="slowestTargets.length === 0" class="no-data">No data available</div>
        <table>
          <thead>
            <tr>
              <th>Target</th>
              <th>Type</th>
              <th>Latency</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="target in slowestTargets" :key="target.id">
              <td>{{ target.target }}</td>
              <td>{{ target.metricType }}</td>
              <td :class="{'text-danger': target.value > 100, 'text-warning': target.value > 50}">
                {{ target.value }} ms
              </td>
              <td>
                <span class="pill soft-pill">
                  <span class="status-dot" :class="target.status === 'Success' ? 'status-success' : 'status-danger'"></span>
                  {{ target.status }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </article>

      <article class="panel table-panel">
        <div class="panel-header">
          <div>
            <p class="panel-title">System Overview</p>
            <p class="panel-subtitle">Current monitoring stats</p>
          </div>
        </div>
        <ul class="location-list">
          <li>
            <div>
              <p class="location-name">Total Targets</p>
              <p class="location-meta text-muted">Active monitored endpoints</p>
            </div>
            <div class="location-score">
              <strong>{{ totalTargets }}</strong>
            </div>
          </li>
          <li>
            <div>
              <p class="location-name">Healthy</p>
              <p class="location-meta text-muted">Targets responding normally</p>
            </div>
            <div class="location-score">
              <strong class="text-success">{{ healthyCount }}</strong>
            </div>
          </li>
          <li>
            <div>
              <p class="location-name">Critical</p>
              <p class="location-meta text-muted">Targets down or high loss</p>
            </div>
            <div class="location-score">
              <strong class="text-danger">{{ criticalCount }}</strong>
            </div>
          </li>
        </ul>
      </article>
    </section>
  </main>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import axios from 'axios'

// --- State ---
const rawData = ref([])
const timer = ref(null)

// --- Config ---
const getApiBaseUrl = () => {
  if (import.meta.env.VITE_API_URL) return import.meta.env.VITE_API_URL
  if (window.location.port === "8080" || window.location.hostname !== "localhost") return "/api"
  return "http://localhost:5050"
}
const API_URL = getApiBaseUrl()

// --- Data Fetching ---
const fetchDashboardData = async () => {
  try {
    // ใช้ API /latest ที่คุณมีอยู่แล้ว เพื่อดึงสถานะล่าสุดของทุก Target
    const endpoint = API_URL.startsWith("/") ? `${API_URL}/metrics/latest` : `${API_URL}/api/metrics/latest`
    const res = await axios.get(endpoint)
    rawData.value = res.data
  } catch (err) {
    console.error("Dashboard fetch error:", err)
  }
}

// --- Computed Properties (Calculations) ---

// 1. Metrics Cards (4 กล่องบน)
const dashboardMetrics = computed(() => {
  if (rawData.value.length === 0) return [
    { label: "Mean Latency", value: "0 ms", progress: 0 },
    { label: "Avg Packet Loss", value: "0%", progress: 0 },
    { label: "Active Targets", value: "0", progress: 0 },
    { label: "System Health", value: "0%", progress: 0 }
  ]

  // Calculate Average Latency
  const totalLatency = rawData.value.reduce((acc, curr) => acc + curr.value, 0)
  const avgLatency = Math.round(totalLatency / rawData.value.length)

  // Calculate Average Packet Loss
  const totalLoss = rawData.value.reduce((acc, curr) => acc + curr.packetLoss, 0)
  const avgLoss = (totalLoss / rawData.value.length).toFixed(2)

  // Success Rate
  const successCount = rawData.value.filter(i => i.status === 'Success').length
  const healthRate = Math.round((successCount / rawData.value.length) * 100)

  return [
    {
      label: "Mean Latency",
      value: `${avgLatency} ms`,
      caption: "Global average",
      delta: avgLatency < 50 ? "Healthy" : "High",
      trendClass: avgLatency < 50 ? "up" : "down", // Reusing 'up' style for green
      progress: Math.min(avgLatency, 100) // Scale to 100
    },
    {
      label: "Avg Packet Loss",
      value: `${avgLoss}%`,
      caption: "Across all targets",
      delta: avgLoss < 1 ? "Good" : "Issues",
      trendClass: avgLoss < 1 ? "up" : "down",
      progress: Math.min(avgLoss * 10, 100)
    },
    {
      label: "Active Targets",
      value: rawData.value.length.toString(),
      caption: "Monitored endpoints",
      delta: "Active",
      trendClass: "up",
      progress: 100
    },
    {
      label: "System Health",
      value: `${healthRate}%`,
      caption: "Success rate",
      delta: healthRate > 90 ? "Stable" : "Degraded",
      trendClass: healthRate > 90 ? "up" : "down",
      progress: healthRate
    }
  ]
})

// 2. Target Health List
const targetHealth = computed(() => {
  return rawData.value.map(item => ({
    ...item,
    statusClass: item.status === 'Success' ? 'status-success' : 'status-danger'
  }))
})

// 3. Slowest Targets (Top 5)
const slowestTargets = computed(() => {
  // Copy array and sort by value descending
  return [...rawData.value]
    .sort((a, b) => b.value - a.value)
    .slice(0, 5)
})

// 4. Counts
const totalTargets = computed(() => rawData.value.length)
const healthyCount = computed(() => rawData.value.filter(i => i.status === 'Success').length)
const criticalCount = computed(() => rawData.value.filter(i => i.status !== 'Success' || i.packetLoss > 0).length)

// --- Lifecycle ---
onMounted(() => {
  fetchDashboardData()
  timer.value = setInterval(fetchDashboardData, 5000) // Auto refresh every 5s
})

onUnmounted(() => clearInterval(timer.value))
</script>

<style scoped>
/* Keeping original styles + minor adjustments for integration */
.workspace {
  padding: 40px clamp(24px, 4vw, 60px);
  color: white;
}

.workspace-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
  margin-bottom: 32px;
}

.eyebrow {
  text-transform: uppercase;
  letter-spacing: 0.2em;
  font-size: 12px;
  color: #8b949e;
}

.title-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

.workspace-header h1 {
  font-size: clamp(28px, 4vw, 36px);
  margin: 8px 0 4px;
}

.metrics-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 20px;
}

.metric-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 8px;
  padding: 20px;
}

.metric-head { display: flex; justify-content: space-between; align-items: center; }
.metric-label { font-size: 13px; text-transform: uppercase; letter-spacing: 0.18em; color: #8b949e; }
.metric-card h2 { font-size: 32px; font-weight: 600; margin: 0; }
.metric-subtitle { color: #8b949e; margin: 0; font-size: 0.9em; }

.trend-pill { border-radius: 999px; padding: 4px 12px; font-size: 13px; }
.trend-pill.up { background: rgba(63, 185, 80, 0.15); color: #3fb950; } /* Green */
.trend-pill.down { background: rgba(248, 81, 73, 0.15); color: #f85149; } /* Red */

.progress-track {
  width: 100%; height: 6px; border-radius: 999px; background: rgba(255, 255, 255, 0.1); overflow: hidden; margin-top: 10px;
}
.progress-fill {
  height: 100%; border-radius: inherit; background: linear-gradient(120deg, #238636, #2ea043);
}

.panels-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 24px;
  margin-top: 32px;
}

.wide-panel { grid-column: span 2; }
.panel { background: #161b22; border: 1px solid #30363d; border-radius: 8px; padding: 20px; }
.panel-header { display: flex; justify-content: space-between; margin-bottom: 20px; }
.panel-title { font-size: 1.1em; font-weight: 600; margin: 0; }
.panel-subtitle { color: #8b949e; font-size: 0.9em; margin: 4px 0 0; }

/* Probe Grid */
.probe-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px; }
.probe-card { border: 1px solid rgba(255, 255, 255, 0.1); border-radius: 8px; padding: 18px; background: rgba(255, 255, 255, 0.02); }
.probe-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
.probe-name { font-weight: 600; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 150px; }
.probe-meta { color: #8b949e; font-size: 13px; margin: 0; }
.probe-metrics { display: flex; justify-content: space-between; margin-top: 12px; }
.probe-label { font-size: 11px; text-transform: uppercase; color: #8b949e; }
.probe-value { font-size: 14px; font-weight: 600; }

/* Table */
table { width: 100%; border-collapse: collapse; }
th, td { padding: 12px 0; text-align: left; border-bottom: 1px solid rgba(255, 255, 255, 0.1); }
th { font-size: 12px; text-transform: uppercase; color: #8b949e; }
tr:last-child td { border-bottom: none; }

/* Location/Stats List */
.location-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 12px; }
.location-list li { display: flex; align-items: center; justify-content: space-between; padding: 12px 0; border-bottom: 1px solid rgba(255, 255, 255, 0.1); }
.location-list li:last-child { border-bottom: none; }
.location-score strong { font-size: 24px; }

/* Utilities */
.text-muted { color: #8b949e; }
.text-danger { color: #f85149; }
.text-warning { color: #d29922; }
.text-success { color: #3fb950; }
.btn-primary { background: #238636; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; }
.btn-ghost { background: transparent; border: 1px solid #30363d; color: #c9d1d9; padding: 4px 10px; border-radius: 6px; cursor: pointer; font-size: 12px; }
.no-data { text-align: center; padding: 40px; color: #8b949e; font-style: italic; }

/* Status Pills */
.pill { padding: 2px 10px; border-radius: 12px; font-size: 0.75em; display: inline-flex; align-items: center; gap: 6px; background: rgba(255,255,255,0.1); }
.status-success { background: rgba(63,185,80,0.2); color: #3fb950; }
.status-danger { background: rgba(248,81,73,0.2); color: #f85149; }
.status-dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
.header-pill { background: rgba(51, 179, 174, 0.15); color: #33b3ae; border: 1px solid rgba(51, 179, 174, 0.3); }

@media (max-width: 960px) {
  .wide-panel { grid-column: span 1; }
  .workspace-header { flex-direction: column; }
}
</style>