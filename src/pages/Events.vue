<template>
  <div class="page-shell">
    <header class="page-header">
      <div>
        <p class="eyebrow">Timeline</p>
        <h1>Network Events</h1>
        <p class="text-muted">
          Latest activities from your monitoring agents.
        </p>
      </div>
      <button class="btn-primary" @click="fetchEvents">
        Refresh Logs
      </button>
    </header>

    <section class="event-list panel">
      <div v-if="events.length === 0" class="no-data">
        No events found in database.
      </div>

      <article v-for="event in events" :key="event.id" class="event-row">
        <div class="event-time">
          <strong>{{ event.timeStr }}</strong>
          <span>{{ event.dateStr }}</span>
        </div>

        <div class="event-body">
          <div class="event-header">
            <div class="event-title-group">
              <h3>{{ event.target }}</h3>
              
              <span class="pill soft-pill">
                {{ event.metricType }}
              </span>

              <span 
                class="pill soft-pill" 
                :class="event.status === 'Success' ? 'status-success' : 'status-danger'"
              >
                <span class="status-dot"></span>
                {{ event.status }}
              </span>
            </div>
          </div>

          <p class="text-muted">
            Latency: <strong style="color: white">{{ event.value }} ms</strong> 
            <span v-if="event.packetLoss > 0"> | Loss: {{ event.packetLoss }}%</span>
            <span v-if="event.statusCode"> | Code: {{ event.statusCode }}</span>
          </p>

          <button class="btn-text" @click="event.showDetails = !event.showDetails">
            {{ event.showDetails ? 'Hide Log' : 'View Raw Log' }}
          </button>

          <div v-if="event.showDetails" class="log-box">
            <pre>{{ JSON.stringify(event.raw, null, 2) }}</pre>
          </div>
        </div>
      </article>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

const events = ref([])

const fetchEvents = async () => {
  try {
    // ดึงข้อมูล 20 ตัวล่าสุด (ใช้ API ตัวเดิมที่มีอยู่แล้ว)
    const res = await axios.get('http://localhost:5050/api/metrics/filter')
    
    // แปลงข้อมูลจาก Backend ให้เป็นรูปแบบที่หน้าเว็บใช้ง่ายๆ
    events.value = res.data.map(item => {
      const dateObj = new Date(item.timestamp)
      return {
        id: item.id,
        target: item.target,
        metricType: item.metricType,
        value: item.value,
        status: item.status,
        packetLoss: item.packetLoss,
        statusCode: item.statusCode,
        
        // จัดรูปแบบเวลา
        timeStr: dateObj.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false }),
        dateStr: dateObj.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
        
        // เก็บข้อมูลดิบไว้โชว์ตอนกด View Log
        raw: item,
        showDetails: false // state สำหรับเปิด/ปิด log
      }
    })
  } catch (err) {
    console.error("Error loading events:", err)
  }
}

// โหลดข้อมูลทันทีเมื่อเข้าหน้านี้
onMounted(() => {
  fetchEvents()
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

.btn-primary {
  background: #238636;
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 600;
}

.btn-text {
  background: none;
  border: none;
  color: #58a6ff;
  cursor: pointer;
  font-size: 0.9em;
  margin-top: 8px;
  padding: 0;
  text-align: left;
}
.btn-text:hover { text-decoration: underline; }

.event-list {
  display: flex;
  flex-direction: column;
  padding: 0;
  background: #161b22; /* พื้นหลังเข้ม */
  border-radius: 8px;
  border: 1px solid #30363d;
}

.event-row {
  display: grid;
  grid-template-columns: 100px 1fr;
  gap: 24px;
  padding: 24px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.event-row:last-child { border-bottom: none; }

.event-time { text-align: right; color: #8b949e; }
.event-time strong { display: block; font-size: 20px; color: white; }

.event-title-group {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  margin-bottom: 8px;
}

h3 { margin: 0; font-size: 1.1em; color: #fff; }

.text-muted { color: #8b949e; margin: 0; }

/* Status Styles */
.pill {
  padding: 4px 12px;
  border-radius: 20px;
  font-size: 0.85em;
  background: rgba(255,255,255,0.1);
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.status-success { color: #3fb950; background: rgba(63, 185, 80, 0.15); }
.status-danger { color: #f85149; background: rgba(248, 81, 73, 0.15); }
.status-dot {
  width: 8px; height: 8px;
  border-radius: 50%;
  background-color: currentColor;
}

/* Log Box */
.log-box {
  background: #0d1117;
  padding: 10px;
  border-radius: 6px;
  margin-top: 10px;
  border: 1px solid #30363d;
  overflow-x: auto;
}
pre { margin: 0; color: #c9d1d9; font-size: 0.85em; font-family: monospace; }
.no-data { padding: 40px; text-align: center; color: #8b949e; font-style: italic; }

@media (max-width: 768px) {
  .event-row { grid-template-columns: 1fr; gap: 10px; }
  .event-time { text-align: left; display: flex; gap: 10px; align-items: baseline; }
}
</style>