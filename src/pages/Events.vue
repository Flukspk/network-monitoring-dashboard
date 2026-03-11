<template>
  <div class="grafana-workspace">
    <header class="grafana-header">
      <div class="header-left">
        <p class="eyebrow">History & Alerts</p>
        <h1 class="dashboard-title">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="color: #f2495c; margin-right: 8px;">
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
            <line x1="12" y1="9" x2="12" y2="13"></line>
            <line x1="12" y1="17" x2="12.01" y2="17"></line>
          </svg>
          System Event Logs
        </h1>
        <p class="text-muted">
          Track network anomalies, recent events, and downtime incidents.
        </p>
      </div>
      <div class="header-right">
        <button 
          class="btn-toggle" 
          :class="{ 'active': showIssuesOnly }"
          @click="showIssuesOnly = !showIssuesOnly"
        >
          <svg v-if="showIssuesOnly" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
          <svg v-else width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
          {{ showIssuesOnly ? 'Showing Issues Only' : 'Show All Events' }}
        </button>
        
        <span v-if="loading" class="refresh-indicator is-refreshing">
          <span class="pulse-dot"></span> Updating...
        </span>
        <button class="btn-grafana" @click="fetchEvents">
          Refresh Data
        </button>
      </div>
    </header>

    <section class="events-container">
      <div v-if="filteredEvents.length === 0 && !loading" class="g-panel no-data">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" style="margin-bottom:16px; opacity:0.5; color: #73bf69;">
          <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline>
        </svg>
        <p style="font-size: 16px; color: #fff;">No events matching your criteria.</p>
        <p v-if="showIssuesOnly" class="text-green" style="margin-top:8px;">Awesome! Your network is 100% healthy right now.</p>
      </div>

      <div class="table-header" v-if="filteredEvents.length > 0">
        <div class="th col-target">TARGET INFO</div>
        <div class="th col-incident">LATEST EVENT / ANOMALY</div>
        <div class="th col-stats">AVG STATS</div>
        <div class="th col-status">STATUS</div>
        <div class="th col-action"></div>
      </div>

      <div 
        v-for="group in filteredEvents" 
        :key="group.target + group.metricType" 
        class="g-panel event-row" 
        :class="{ 'expanded': group.expanded, 'has-issue': group.hasError }"
      >
        
        <div class="main-row" @click="toggleGroup(group)">
          <div class="col-target">
            <div class="target-name">{{ group.target }}</div>
            <div class="target-meta">
              <span class="g-badge bg-blue-dim">{{ group.metricType }}</span>
              <span class="badge-count">{{ group.logs.length }} records</span>
            </div>
          </div>

          <div class="col-incident">
            <div v-if="group.hasError && group.lastIssue" class="incident-box">
              <span class="incident-label text-red">Incident Detected:</span>
              <span class="incident-msg">{{ group.lastIssue.message }}</span>
              <span class="incident-time">Latest: {{ group.lastIssue.timeStr }}</span>
            </div>
            <div v-else class="incident-box healthy">
              <span class="incident-label text-green">Stable Operation</span>
              <span class="incident-msg text-muted">No issues detected in the recent logs.</span>
              <span class="incident-time">Last check: {{ group.lastTime }}</span>
            </div>
          </div>

          <div class="col-stats">
            <div class="stat-item">
               <span class="label">Lat:</span>
               <span class="value">{{ group.avgLatency }}ms</span>
            </div>
            <div class="stat-item">
               <span class="label">Loss:</span>
               <span class="value" :class="{'text-red font-bold': group.avgLoss > 0}">{{ group.avgLoss }}%</span>
            </div>
          </div>

          <div class="col-status">
            <span class="status-pill" :class="group.hasError ? 'down' : 'up'">
              <span class="status-dot"></span>
              {{ group.hasError ? 'Action Required' : 'Healthy' }}
            </span>
          </div>

          <div class="col-action">
             <span class="arrow">{{ group.expanded ? '▼' : '▶' }}</span>
          </div>
        </div>

        <div v-if="group.expanded" class="detail-pane">
          <table class="g-table">
            <thead>
              <tr>
                <th>Time</th>
                <th>Latency</th>
                <th>Status</th>
                <th>Event Detail</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="log in group.logs" :key="log.id" :class="{'row-error': log.status !== 'Success'}">
                <td class="td-time">{{ log.timeStr }}</td>
                <td class="td-val text-blue">
                  {{ log.value }} ms
                </td>
                <td>
                  <span class="g-badge" :class="log.status === 'Success' ? 'bg-green' : 'bg-red'">
                    {{ log.status }}
                  </span>
                </td>
                <td class="td-msg" :class="{'text-red': log.status !== 'Success'}">
                  {{ log.message }}
                  <span v-if="group.metricType === 'TRACEROUTE'" class="hint-msg">
                    (Check Dashboard for full hop routing)
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'

const groupedEvents = ref([])
const loading = ref(false)
const showIssuesOnly = ref(false) // ตัวแปรสำหรับปุ่ม Filter

// Config API
const getApiBaseUrl = () => {
  if (import.meta.env.VITE_API_URL) return import.meta.env.VITE_API_URL
  if (window.location.port === "8080" || window.location.hostname !== "localhost") return "/api"
  return "http://localhost:5050"
}
const API_URL = `${getApiBaseUrl()}/metrics/filter`

const fetchEvents = async () => {
  loading.value = true
  try {
    const res = await axios.get(API_URL) 
    const rawData = res.data

    const groups = {}

    rawData.forEach(item => {
      const dateObj = new Date(item.timestamp)
      const timeStr = dateObj.toLocaleTimeString('en-US', { hour12: false })
      const key = `${item.target}_${item.metricType}`
      
      if (!groups[key]) {
        groups[key] = {
          target: item.target,
          metricType: item.metricType,
          logs: [],
          expanded: false,
          totalLatency: 0,
          totalLoss: 0,
          hasError: false,
          lastIssue: null, // 🚨 เก็บข้อมูลเหตุการณ์พังครั้งล่าสุด
          minTs: dateObj.getTime(),
          maxTs: dateObj.getTime(),
          lastTime: timeStr
        }
      }

      const g = groups[key]
      
      // ถอดรหัส Message จาก Backend
      let message = '-'
      try {
          const extra = JSON.parse(item.extraData || '{}')
          message = extra.Message || item.status
      } catch (e) {}

      // 🚨 ตรวจจับ Event ผิดปกติ
      if (item.status !== 'Success' && item.status !== 'Pending') {
          g.hasError = true
          // ถ้ายังไม่มี lastIssue หรือ event นี้เกิดทีหลัง ให้เอาอันนี้เป็นล่าสุด
          if (!g.lastIssue || dateObj.getTime() > g.lastIssue.timestamp) {
              g.lastIssue = { message, timeStr, timestamp: dateObj.getTime() }
          }
      }

      g.logs.push({
        id: item.id,
        value: item.value,
        status: item.status,
        timeStr: timeStr,
        timestamp: dateObj.getTime(),
        message: message
      })

      g.totalLatency += item.value
      g.totalLoss += item.packetLoss

      if (dateObj.getTime() > g.maxTs) {
          g.maxTs = dateObj.getTime()
          g.lastTime = timeStr
      }
    })

    groupedEvents.value = Object.values(groups).map(g => {
        const count = g.logs.length
        return {
            ...g,
            avgLatency: (g.totalLatency / count).toFixed(0),
            avgLoss: (g.totalLoss / count * 100).toFixed(0),
            logs: g.logs.sort((a,b) => b.timestamp - a.timestamp) 
        }
    }).sort((a,b) => b.maxTs - a.maxTs) 

  } catch (err) {
    console.error("Fetch error", err)
  } finally {
    loading.value = false
  }
}

// 🔴 กรองเอาเฉพาะตัวที่พังมาโชว์ ถ้ากดปุ่ม Filter
const filteredEvents = computed(() => {
  if (showIssuesOnly.value) {
    return groupedEvents.value.filter(g => g.hasError);
  }
  return groupedEvents.value;
})

const toggleGroup = (group) => {
    group.expanded = !group.expanded
}

onMounted(() => {
  fetchEvents()
})
</script>

<style scoped>
/* =========================================
   GRAFANA THEME STYLES FOR EVENTS LOG
========================================= */
.grafana-workspace {
  background-color: #111217;
  color: #c7d0d9;
  font-family: Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
  min-height: 100vh;
  padding: 24px 32px;
}

.grafana-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px; }
.eyebrow { font-size: 12px; text-transform: uppercase; letter-spacing: 0.1em; color: #8e99a8; font-weight: 600; margin-bottom: 6px; }
.dashboard-title { display: flex; align-items: center; font-size: 24px; font-weight: 500; color: #fff; margin: 0 0 8px 0; }
.text-muted { color: #8e99a8; font-size: 14px; margin: 0; }
.header-right { display: flex; align-items: center; gap: 16px; margin-top: 20px; }

/* Buttons & Indicators */
.btn-grafana { background: #22252b; border: 1px solid #303540; color: #c7d0d9; padding: 8px 16px; border-radius: 4px; cursor: pointer; transition: background 0.2s; font-weight: 500;}
.btn-grafana:hover { background: #303540; color: #fff; }

.btn-toggle { display: flex; align-items: center; gap: 8px; background: transparent; color: #8e99a8; border: 1px solid #303540; padding: 8px 16px; border-radius: 4px; cursor: pointer; font-weight: 500; transition: all 0.2s; }
.btn-toggle:hover { border-color: #c7d0d9; color: #c7d0d9; }
.btn-toggle.active { background: rgba(242, 73, 92, 0.1); border-color: rgba(242, 73, 92, 0.4); color: #f2495c; }

.refresh-indicator { display: flex; align-items: center; gap: 8px; font-size: 13px; color: #5794f2; }
.pulse-dot { width: 8px; height: 8px; background-color: #5794f2; border-radius: 50%; animation: pulse 1s infinite; }
@keyframes pulse { 0% { transform: scale(0.9); opacity: 1; } 50% { transform: scale(1.5); opacity: 0.5; } 100% { transform: scale(0.9); opacity: 1; } }

/* Table Columns */
.events-container { display: flex; flex-direction: column; gap: 8px; margin-top: 10px; }
.table-header { display: flex; padding: 0 20px 10px 20px; }
.th { font-size: 11px; color: #3274d9; font-weight: 600; letter-spacing: 0.05em; text-transform: uppercase; }

.col-target { flex: 1.5; min-width: 180px; }
.col-incident { flex: 2.5; padding: 0 15px; }
.col-stats { flex: 1; }
.col-status { flex: 1; display: flex; justify-content: flex-end; }
.col-action { width: 30px; display: flex; justify-content: flex-end; }

/* Event Row (Grafana Panel Style) */
.g-panel { background-color: #181b1f; border: 1px solid #22252b; border-radius: 4px; position: relative; display: flex; flex-direction: column; transition: all 0.2s; }
.event-row { border-left: 3px solid #3274d9; }
.event-row:hover { border-color: #444a57; border-left-color: #5794f2; }
.event-row.expanded { border-color: #5794f2; }
/* 🔴 แถบสีแดงไฮไลท์ตัวที่มีปัญหา */
.event-row.has-issue { border-left-color: #f2495c; background: linear-gradient(90deg, rgba(242, 73, 92, 0.05) 0%, rgba(24, 27, 31, 1) 15%); }
.event-row.has-issue:hover { border-color: #f2495c; }

.main-row { display: flex; align-items: center; padding: 16px 20px; cursor: pointer; }

/* 1. Target */
.target-name { font-weight: 500; font-size: 15px; color: #fff; margin-bottom: 6px; }
.target-meta { display: flex; align-items: center; gap: 8px; }
.badge-count { font-size: 11px; color: #8e99a8; }
.g-badge { padding: 2px 6px; border-radius: 4px; font-size: 10px; font-weight: bold; color: #fff; letter-spacing: 0.5px; }
.bg-blue-dim { background: rgba(50, 116, 217, 0.2); color: #5794f2; border: 1px solid rgba(50, 116, 217, 0.3); }

/* 🚨 2. Incident Box (บอกว่าเกิดอะไรขึ้น) */
.incident-box { display: flex; flex-direction: column; gap: 3px; }
.incident-label { font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; }
.incident-msg { font-size: 14px; color: #fff; font-weight: 500; }
.incident-time { font-size: 11px; color: #8e99a8; font-family: monospace; }

/* 3. Stats */
.col-stats { display: flex; gap: 20px; font-size: 13px; }
.stat-item { display: flex; flex-direction: column; }
.stat-item .label { font-size: 11px; color: #8e99a8; margin-bottom: 2px;}
.stat-item .value { font-weight: 500; color: #c7d0d9; font-family: monospace; font-size: 14px; }

/* 4. Status Pill */
.status-pill { padding: 4px 12px; border-radius: 20px; font-size: 12px; display: inline-flex; align-items: center; gap: 6px; font-weight: 500; }
.up { color: #73bf69; background: rgba(115, 191, 105, 0.1); border: 1px solid rgba(115, 191, 105, 0.2); }
.down { color: #f2495c; background: rgba(242, 73, 92, 0.1); border: 1px solid rgba(242, 73, 92, 0.2); }
.status-dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
.arrow { font-size: 12px; color: #8e99a8; }

/* Colors */
.text-red { color: #f2495c !important; }
.text-green { color: #73bf69 !important; }
.text-blue { color: #5794f2 !important; }
.font-bold { font-weight: bold; }
.bg-green { background-color: #73bf69 !important; }
.bg-red { background-color: #f2495c !important; }

/* --- Detail Pane (ตารางด้านใน) --- */
.detail-pane { border-top: 1px solid #22252b; background: #111217; padding: 10px 20px; }
.g-table { width: 100%; border-collapse: collapse; text-align: left; font-size: 13px; }
.g-table th { padding: 8px 12px; color: #3274d9; font-weight: 500; border-bottom: 1px solid #22252b; }
.g-table td { padding: 10px 12px; border-bottom: 1px solid #22252b; color: #c7d0d9; }
.row-error td { background: rgba(242, 73, 92, 0.05); }
.g-table tbody tr:last-child td { border-bottom: none; }

.td-time { color: #8e99a8; width: 120px; font-family: monospace; }
.td-val { font-weight: 500; width: 100px; font-family: monospace; }
.td-msg { color: #c7d0d9; }
.hint-msg { font-size: 11px; font-style: italic; color: #8e99a8; margin-left: 6px; }

.no-data { text-align: center; color: #8e99a8; padding: 40px; border-style: dashed; align-items: center; justify-content: center; }
</style>