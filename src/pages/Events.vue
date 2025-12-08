<template>
  <div class="page-shell">
    <header class="page-header">
      <div>
        <p class="eyebrow">Timeline</p>
        <h1>Network Events (Grouped)</h1>
        <p class="text-muted">
          Grouped activities by target. Click to see details.
        </p>
      </div>
      <button class="btn-primary" @click="fetchEvents">
        Refresh Logs
      </button>
    </header>

    <section class="event-list panel">
      <div v-if="groupedEvents.length === 0" class="no-data">
        Waiting for monitoring data...
      </div>

      <article v-for="group in groupedEvents" :key="group.target" class="event-group">
        
        <div class="group-header" @click="group.expanded = !group.expanded">
          <div class="group-info">
            <h3>{{ group.target }}</h3>
            <span class="pill soft-pill">{{ group.metricType }}</span>
            
            <span class="pill soft-pill" :class="group.latestStatus === 'Success' ? 'status-success' : 'status-danger'">
              <span class="status-dot"></span>
              {{ group.latestStatus }}
            </span>
          </div>

          <div class="group-meta">
            <span class="time-range">
              {{ group.startTime }} - {{ group.lastTime }}
            </span>
            <span class="expand-icon">{{ group.expanded ? '▼' : '▶' }}</span>
          </div>
        </div>

        <div v-if="group.expanded" class="group-body">
          <table class="log-table">
            <thead>
              <tr>
                <th>Time</th>
                <th>Latency</th>
                <th>Status</th>
                <th>Raw</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="log in group.logs" :key="log.id">
                <td class="col-time">{{ log.timeStr }}</td>
                <td>
                  <span class="val">{{ log.value }} ms</span>
                  <span v-if="log.packetLoss > 0" class="text-danger"> (Loss {{ log.packetLoss }}%)</span>
                </td>
                <td>
                  <span :class="log.status === 'Success' ? 'text-green' : 'text-red'">
                    {{ log.status }}
                  </span>
                </td>
                <td>
                  <button class="btn-link" @click="log.showJson = !log.showJson">
                    {{ log.showJson ? '{}' : '{...}' }}
                  </button>
                  <div v-if="log.showJson" class="mini-json">
                    {{ JSON.stringify(log.raw, null, 0) }}
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

      </article>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

// เก็บข้อมูลที่จัดกลุ่มแล้ว
const groupedEvents = ref([])

const fetchEvents = async () => {
  try {
    // ดึงข้อมูลดิบมาก่อน (อาจจะขอเยอะหน่อย เช่น 50 ตัวล่าสุด เพื่อให้เห็นกลุ่มชัดเจน)
    // หมายเหตุ: Backend ของคุณตอนนี้ตั้ง limit ไว้ 20 ใน MetricsController (Take(20)) 
    // ถ้าอยากได้เยอะกว่านี้ต้องแก้ Backend แต่ตอนนี้ใช้ 20 ไปก่อนได้ครับ
    const res = await axios.get('http://localhost:5050/api/metrics/filter')
    const rawData = res.data

    // --- LOGIC การจัดกลุ่ม (Grouping) ---
    const groups = {}

    rawData.forEach(item => {
      // แปลงวันที่
      const dateObj = new Date(item.timestamp)
      const timeStr = dateObj.toLocaleTimeString('en-US', { hour12: false })
      
      // ถ้ายังไม่มีกลุ่มของ Target นี้ ให้สร้างใหม่
      if (!groups[item.target]) {
        groups[item.target] = {
          target: item.target,
          metricType: item.metricType,
          logs: [],
          expanded: false, // สถานะเปิด/ปิด
          latestStatus: item.status, // เอาสถานะตัวล่าสุด (ตัวแรกที่เจอ)
          lastTime: timeStr, // เวลาล่าสุด
          startTime: timeStr // เดี๋ยวจะอัปเดตตอนวนลูปจบ
        }
      }

      // ยัดข้อมูลย่อยลงไปในกลุ่ม
      groups[item.target].logs.push({
        id: item.id,
        value: item.value,
        status: item.status,
        packetLoss: item.packetLoss,
        timeStr: timeStr,
        timestamp: dateObj.getTime(), // เก็บ timestamp ไว้เทียบเวลา
        raw: item,
        showJson: false
      })
      
      // อัปเดตเวลาเริ่มต้น (Start Time) ให้เป็นเวลาของข้อมูลที่เก่าที่สุดในชุด
      groups[item.target].startTime = timeStr 
    })

    // แปลง Object เป็น Array เพื่อเอาไปวนลูปแสดงผล
    groupedEvents.value = Object.values(groups)

  } catch (err) {
    console.error("Failed to fetch events", err)
  }
}

onMounted(() => {
  fetchEvents()
  setInterval(fetchEvents, 5000) 
})
</script>

<style scoped>
.page-shell { padding: 40px; color: #fff; }
.page-header { display: flex; justify-content: space-between; margin-bottom: 20px; }

.btn-primary { background: #238636; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; }

/* Group Styles */
.event-list { display: flex; flex-direction: column; gap: 15px; }

.event-group {
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 8px;
  overflow: hidden;
}

/* Header ของแต่ละกลุ่ม (แถบที่คลิกได้) */
.group-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  cursor: pointer;
  background: #21262d;
  transition: background 0.2s;
}
.group-header:hover { background: #30363d; }

.group-info { display: flex; align-items: center; gap: 12px; }
.group-info h3 { margin: 0; color: #58a6ff; font-size: 1.1em; }

.group-meta { display: flex; align-items: center; gap: 15px; color: #8b949e; font-size: 0.9em; }
.expand-icon { font-size: 0.8em; }

/* Body ของกลุ่ม (ตาราง Logs) */
.group-body {
  border-top: 1px solid #30363d;
  background: #0d1117;
  padding: 10px;
}

/* Table Styles */
.log-table { width: 100%; border-collapse: collapse; font-size: 0.9em; }
.log-table th { text-align: left; color: #8b949e; padding: 8px; border-bottom: 1px solid #30363d; }
.log-table td { padding: 8px; border-bottom: 1px solid #21262d; color: #c9d1d9; }
.log-table tr:last-child td { border-bottom: none; }

.col-time { width: 120px; color: #8b949e; }
.val { font-weight: bold; color: #e6edf3; }
.text-green { color: #3fb950; }
.text-red { color: #f85149; }
.text-danger { color: #f85149; font-size: 0.8em; }

/* ปุ่ม Raw Json */
.btn-link { background: none; border: none; color: #58a6ff; cursor: pointer; font-family: monospace; }
.mini-json { font-size: 0.75em; color: #8b949e; margin-top: 4px; font-family: monospace; word-break: break-all; }

/* Status Pills */
.pill { padding: 2px 10px; border-radius: 12px; font-size: 0.75em; display: inline-flex; align-items: center; gap: 6px; background: rgba(255,255,255,0.1); }
.status-success { color: #3fb950; background: rgba(63, 185, 80, 0.15); }
.status-danger { color: #f85149; background: rgba(248, 81, 73, 0.15); }
.status-dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }

.no-data { text-align: center; color: #8b949e; padding: 40px; }
</style>