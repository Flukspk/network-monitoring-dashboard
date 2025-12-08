<template>
  <div class="page-shell">
    <header class="page-header">
      <div>
        <p class="eyebrow">Alert queue</p>
        <h1>Active notifications</h1>
        <p class="text-muted">
          Real-time alerts based on latency threshold (>200ms) and connection failures.
        </p>
      </div>
      <div class="header-actions">
        <button class="btn-ghost" @click="fetchAlerts">Refresh</button>
        <button class="btn-primary" @click="alerts = []">Acknowledge all</button>
      </div>
    </header>

    <section class="alerts-grid">
      <div v-if="alerts.length === 0" class="no-data-panel">
        <span class="icon-check">✓</span>
        <h3>All systems normal</h3>
        <p class="text-muted">No active alerts detected at this time.</p>
      </div>

      <article class="panel alert-card" v-for="alert in alerts" :key="alert.id">
        <div class="alert-head">
          <span class="pill soft-pill" :class="alert.bgClass">
            <span class="status-dot" :class="alert.dotClass"></span>
            {{ alert.severity }}
          </span>
          <span class="alert-time text-muted">{{ alert.timeAgo }}</span>
        </div>
        
        <h3>{{ alert.title }}</h3>
        <p class="text-muted">{{ alert.detail }}</p>
        
        <div class="alert-foot">
          <span class="target-badge">{{ alert.target }}</span>
          <span class="type-badge">{{ alert.type }}</span>
        </div>
      </article>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

const alerts = ref([])

// --- Config ---
const getApiBaseUrl = () => {
  if (import.meta.env.VITE_API_URL) return import.meta.env.VITE_API_URL
  if (window.location.port === "8080" || window.location.hostname !== "localhost") return "/api"
  return "http://localhost:5050"
}
const API_URL = getApiBaseUrl()

// --- Helper: คำนวณเวลา ---
const timeAgo = (date) => {
  const seconds = Math.floor((new Date() - new Date(date)) / 1000)
  let interval = seconds / 31536000
  if (interval > 1) return Math.floor(interval) + " years ago"
  interval = seconds / 2592000
  if (interval > 1) return Math.floor(interval) + " months ago"
  interval = seconds / 86400
  if (interval > 1) return Math.floor(interval) + " days ago"
  interval = seconds / 3600
  if (interval > 1) return Math.floor(interval) + " hours ago"
  interval = seconds / 60
  if (interval > 1) return Math.floor(interval) + " mins ago"
  return Math.floor(seconds) + " seconds ago"
}

// --- Logic การสร้าง Alert ---
const fetchAlerts = async () => {
  try {
    // ดึง 50 รายการล่าสุดมาตรวจสุขภาพ
    const endpoint = API_URL.startsWith("/") ? `${API_URL}/metrics/filter` : `${API_URL}/api/metrics/filter`
    const res = await axios.get(endpoint)
    const data = res.data

    const generatedAlerts = []

    data.forEach(item => {
      // เงื่อนไขที่ 1: Connection Failed (Critical)
      if (item.status !== 'Success') {
        generatedAlerts.push({
          id: item.id,
          severity: "Critical",
          title: "Connection Failed",
          detail: `Agent failed to reach target. Status code: ${item.statusCode || 'N/A'}.`,
          target: item.target,
          type: item.metricType,
          timeAgo: timeAgo(item.timestamp),
          bgClass: 'bg-danger',
          dotClass: 'status-danger'
        })
      }
      // เงื่อนไขที่ 2: Packet Loss (Critical)
      else if (item.packetLoss > 0) {
        generatedAlerts.push({
          id: item.id,
          severity: "Critical",
          title: "Packet Loss Detected",
          detail: `Packet loss detected at ${item.packetLoss}%. Connection unstable.`,
          target: item.target,
          type: item.metricType,
          timeAgo: timeAgo(item.timestamp),
          bgClass: 'bg-danger',
          dotClass: 'status-danger'
        })
      }
      // เงื่อนไขที่ 3: High Latency (Warning) เกิน 200ms
      else if (item.value > 200) {
        generatedAlerts.push({
          id: item.id,
          severity: "Warning",
          title: "High Latency",
          detail: `Response time is ${item.value}ms, which exceeds the 200ms threshold.`,
          target: item.target,
          type: item.metricType,
          timeAgo: timeAgo(item.timestamp),
          bgClass: 'bg-warning',
          dotClass: 'status-warning'
        })
      }
    })

    alerts.value = generatedAlerts

  } catch (err) {
    console.error("Alert fetch error:", err)
  }
}

onMounted(() => {
  fetchAlerts()
  // เช็คทุก 10 วินาที
  setInterval(fetchAlerts, 10000)
})
</script>

<style scoped>
.page-shell {
  padding: 48px clamp(24px, 6vw, 72px);
  display: flex;
  flex-direction: column;
  gap: 28px;
  color: white;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 20px;
}

.header-actions {
  display: flex;
  gap: 12px;
}

h1 {
  margin: 6px 0;
  font-size: clamp(28px, 4vw, 36px);
}

.alerts-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 20px;
}

.alert-card {
  display: flex;
  flex-direction: column;
  gap: 14px;
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 8px;
  padding: 20px;
  transition: transform 0.2s;
}

.alert-card:hover {
  border-color: #58a6ff;
  transform: translateY(-2px);
}

.alert-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.alert-time { font-size: 0.85em; }

h3 { margin: 0; font-size: 1.1em; color: #fff; }

.alert-foot {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: auto;
  padding-top: 10px;
  border-top: 1px solid rgba(255,255,255,0.1);
}

.target-badge { font-weight: 600; color: #58a6ff; }
.type-badge { font-size: 0.8em; color: #8b949e; text-transform: uppercase; letter-spacing: 1px; }

/* Buttons */
.btn-primary { background: #238636; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; }
.btn-ghost { background: transparent; border: 1px solid #30363d; color: #c9d1d9; padding: 8px 16px; border-radius: 6px; cursor: pointer; }

/* Status Styles */
.pill { padding: 4px 12px; border-radius: 20px; font-size: 0.85em; display: inline-flex; align-items: center; gap: 6px; font-weight: 600; }
.bg-danger { background: rgba(248, 81, 73, 0.15); color: #ff6b6b; }
.bg-warning { background: rgba(210, 153, 34, 0.15); color: #e3b341; }
.status-dot { width: 8px; height: 8px; border-radius: 50%; background-color: currentColor; }

/* No Data State */
.no-data-panel {
  grid-column: 1 / -1;
  background: #161b22;
  border: 1px dashed #30363d;
  border-radius: 8px;
  padding: 60px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
}
.icon-check {
  width: 60px; height: 60px;
  background: rgba(63, 185, 80, 0.15);
  color: #3fb950;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32px;
}

@media (max-width: 768px) {
  .page-header { flex-direction: column; }
  .header-actions { width: 100%; justify-content: flex-start; }
}
</style>