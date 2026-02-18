<template>
  <div class="page-shell">
    <header class="page-header">
      <div>
        <p class="eyebrow">History</p>
        <h1>Network Activity Log</h1>
        <p class="text-muted">
          Grouped by target. Shows summary of recent activities.
        </p>
      </div>
      <div class="header-actions">
         <span v-if="loading" class="loading-text">Updating...</span>
         <button class="btn-primary" @click="fetchEvents">
          Refresh Data
        </button>
      </div>
    </header>

    <section class="panel">
      <div v-if="groupedEvents.length === 0 && !loading" class="no-data">
        No logs found. Run some tests first.
      </div>

      <div class="table-header" v-if="groupedEvents.length > 0">
        <div class="th col-target">TARGET</div>
        <div class="th col-stats">SUMMARY (AVG)</div>
        <div class="th col-time">LAST SEEN</div>
        <div class="th col-status">STATUS</div>
        <div class="th col-action"></div>
      </div>

      <div 
        v-for="group in groupedEvents" 
        :key="group.target" 
        class="event-row" 
        :class="{ 'expanded': group.expanded }"
      >
        
        <div class="main-row" @click="toggleGroup(group)">
          <div class="col-target">
            <div class="target-name">{{ group.target }}</div>
            <div class="badge-count">{{ group.logs.length }} logs</div>
            <span class="metric-tag">{{ group.metricType }}</span>
          </div>

          <div class="col-stats">
            <div class="stat-item">
               <span class="label">Latency:</span>
               <span class="value">{{ group.avgLatency }}ms</span>
            </div>
            <div class="stat-item" v-if="group.avgLoss > 0">
               <span class="label text-danger">Loss:</span>
               <span class="value text-danger">{{ group.avgLoss }}%</span>
            </div>
          </div>

          <div class="col-time">
            {{ group.lastTime }}
            <span class="sub-text">Started: {{ group.startTime }}</span>
          </div>

          <div class="col-status">
            <span class="pill" :class="group.hasError ? 'status-danger' : 'status-success'">
              <span class="status-dot"></span>
              {{ group.hasError ? 'Issues Found' : 'Healthy' }}
            </span>
          </div>

          <div class="col-action">
             <span class="arrow">{{ group.expanded ? '▼' : '▶' }}</span>
          </div>
        </div>

        <div v-if="group.expanded" class="detail-pane">
          <table class="detail-table">
            <thead>
              <tr>
                <th>Time</th>
                <th>Latency</th>
                <th>Status</th>
                <th>Message</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="log in group.logs" :key="log.id">
                <td class="td-time">{{ log.timeStr }}</td>
                <td class="td-val">
                  {{ log.value }} ms
                </td>
                <td>
                  <span :class="log.status === 'Success' ? 'text-green' : 'text-red'">
                    {{ log.status }}
                  </span>
                </td>
                <td class="td-msg">{{ log.message }}</td>
              </tr>
            </tbody>
          </table>
        </div>

      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

const groupedEvents = ref([])
const loading = ref(false)

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
    // ดึงมาสัก 50-100 ตัวล่าสุดเพื่อให้เห็นภาพรวม
    const res = await axios.get(API_URL) 
    const rawData = res.data

    // --- Logic จัดกลุ่ม (Grouping Logic) ---
    const groups = {}

    // 1. วนลูปเพื่อยัดลงกลุ่มตาม Target
    rawData.forEach(item => {
      const dateObj = new Date(item.timestamp)
      const timeStr = dateObj.toLocaleTimeString('en-US', { hour12: false })
      
      // ถ้ายังไม่มีกลุ่ม ให้สร้างใหม่
      if (!groups[item.target]) {
        groups[item.target] = {
          target: item.target,
          metricType: item.metricType,
          logs: [],
          expanded: false,
          totalLatency: 0,
          totalLoss: 0,
          hasError: false,
          // เก็บ Timestamp เพื่อหา min/max time
          minTs: dateObj.getTime(),
          maxTs: dateObj.getTime(),
          startTime: timeStr,
          lastTime: timeStr
        }
      }

      const g = groups[item.target]
      
      // Parse ExtraData เพื่อหา Message
      let message = '-'
      try {
          const extra = JSON.parse(item.extraData || '{}')
          message = extra.Message || item.status
      } catch (e) {}

      // ใส่ข้อมูลย่อย
      g.logs.push({
        id: item.id,
        value: item.value,
        status: item.status,
        timeStr: timeStr,
        timestamp: dateObj.getTime(),
        message: message
      })

      // คำนวณค่าสะสม (เพื่อหาค่าเฉลี่ย)
      g.totalLatency += item.value
      g.totalLoss += item.packetLoss
      if (item.status !== 'Success') g.hasError = true

      // Update เวลาเริ่ม-จบ
      if (dateObj.getTime() < g.minTs) {
          g.minTs = dateObj.getTime()
          g.startTime = timeStr
      }
      if (dateObj.getTime() > g.maxTs) {
          g.maxTs = dateObj.getTime()
          g.lastTime = timeStr
      }
    })

    // 2. แปลงเป็น Array และคำนวณ Average
    groupedEvents.value = Object.values(groups).map(g => {
        const count = g.logs.length
        return {
            ...g,
            avgLatency: (g.totalLatency / count).toFixed(0),
            avgLoss: (g.totalLoss / count * 100).toFixed(0),
            // เรียง log ข้างในจากใหม่ไปเก่า
            logs: g.logs.sort((a,b) => b.timestamp - a.timestamp) 
        }
    }).sort((a,b) => b.maxTs - a.maxTs) // เรียงกลุ่มตามเวลาล่าสุด

  } catch (err) {
    console.error("Fetch error", err)
  } finally {
    loading.value = false
  }
}

const toggleGroup = (group) => {
    group.expanded = !group.expanded
}

onMounted(() => {
  fetchEvents()
})
</script>

<style scoped>
.page-shell { padding: 40px; color: #fff; min-height: 100vh; }
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 30px; }
.header-actions { display: flex; align-items: center; gap: 15px; }

.eyebrow { font-size: 12px; text-transform: uppercase; letter-spacing: 0.1em; color: #8b949e; font-weight: 600; margin-bottom: 4px; }
h1 { margin: 0 0 8px 0; font-size: 24px; }
.text-muted { color: #8b949e; font-size: 14px; margin: 0; }
.loading-text { font-size: 12px; color: #58a6ff; animation: pulse 1.5s infinite; }

.btn-primary { background: #238636; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; font-weight: 600; transition: background 0.2s; }
.btn-primary:hover { background: #2ea043; }

/* --- Table Structure --- */
.panel { display: flex; flex-direction: column; gap: 8px; }

/* Header Columns */
.table-header { display: flex; padding: 0 20px 10px 20px; border-bottom: 1px solid #30363d; margin-bottom: 10px; }
.th { font-size: 11px; color: #8b949e; font-weight: 600; letter-spacing: 0.05em; }

/* Columns Widths */
.col-target { flex: 2; min-width: 200px; }
.col-stats { flex: 1.5; }
.col-time { flex: 1.5; }
.col-status { flex: 1; display: flex; justify-content: flex-end; }
.col-action { width: 30px; display: flex; justify-content: flex-end; }

/* Event Row (Card) */
.event-row {
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 6px;
  overflow: hidden;
  transition: border-color 0.2s;
}
.event-row:hover { border-color: #8b949e; }
.event-row.expanded { border-color: #58a6ff; }

/* Main Row Content */
.main-row {
  display: flex;
  align-items: center;
  padding: 16px 20px;
  cursor: pointer;
}

/* 1. Target Column */
.target-name { font-weight: 600; font-size: 15px; color: #e6edf3; margin-bottom: 4px; }
.badge-count { display: inline-block; background: rgba(110, 118, 129, 0.4); padding: 2px 6px; border-radius: 10px; font-size: 11px; color: #c9d1d9; margin-right: 8px; }
.metric-tag { font-size: 11px; color: #58a6ff; font-family: monospace; border: 1px solid rgba(88, 166, 255, 0.3); padding: 1px 4px; border-radius: 4px; }

/* 2. Stats Column */
.col-stats { display: flex; gap: 15px; font-size: 13px; }
.stat-item { display: flex; flex-direction: column; }
.stat-item .label { font-size: 11px; color: #8b949e; }
.stat-item .value { font-weight: 600; color: #c9d1d9; }
.text-danger { color: #f85149 !important; }

/* 3. Time Column */
.col-time { font-size: 13px; color: #c9d1d9; display: flex; flex-direction: column; }
.sub-text { font-size: 11px; color: #8b949e; }

/* 4. Status Pill */
.pill { padding: 4px 12px; border-radius: 20px; font-size: 12px; display: inline-flex; align-items: center; gap: 6px; font-weight: 500; }
.status-success { color: #3fb950; background: rgba(63, 185, 80, 0.1); border: 1px solid rgba(63, 185, 80, 0.2); }
.status-danger { color: #f85149; background: rgba(248, 81, 73, 0.1); border: 1px solid rgba(248, 81, 73, 0.2); }
.status-dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }

.arrow { font-size: 12px; color: #8b949e; }

/* --- Detail Pane (Expanded) --- */
.detail-pane {
  border-top: 1px solid #30363d;
  background: #0d1117;
  padding: 0;
  animation: slideDown 0.2s ease-out;
}

.detail-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.detail-table th { text-align: left; padding: 10px 20px; color: #8b949e; border-bottom: 1px solid #21262d; font-weight: 500; }
.detail-table td { padding: 10px 20px; border-bottom: 1px solid #21262d; color: #c9d1d9; }
.detail-table tr:last-child td { border-bottom: none; }

.td-time { color: #8b949e; width: 120px; font-family: monospace; }
.td-val { font-weight: 600; width: 100px; }
.td-msg { color: #8b949e; font-style: italic; }

.text-green { color: #3fb950; }
.text-red { color: #f85149; }

@keyframes slideDown { from { opacity: 0; transform: translateY(-5px); } to { opacity: 1; transform: translateY(0); } }
@keyframes pulse { 0% { opacity: 0.5; } 50% { opacity: 1; } 100% { opacity: 0.5; } }

.no-data { text-align: center; color: #8b949e; padding: 60px; font-style: italic; border: 1px dashed #30363d; border-radius: 8px; }
</style>