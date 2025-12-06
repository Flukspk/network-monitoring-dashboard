<template>
  <div class="page-shell">
    <header class="page-header">
      <div>
        <p class="eyebrow">Real-time Monitor</p>
        <h1>Agent Live Graph</h1>
        <p class="text-muted">Monitor latency from your 3 agents.</p>
      </div>
      <button 
        class="btn-primary" 
        @click="toggleMonitoring"
        :class="{ 'btn-danger': isMonitoring }"
      >
        {{ isMonitoring ? 'Stop Monitoring' : 'Start Monitoring' }}
      </button>
    </header>

    <div v-if="!isMonitoring" class="panel config-panel">
      <div class="form-group">
        <label>Select Agent:</label>
        <select v-model="selectedAgentMode">
          <option value="PING">Agent 1 (Ping)</option>
          <option value="HTTP">Agent 2 (HTTP)</option>
          <option value="TRACEROUTE">Agent 3 (Trace)</option>
        </select>
      </div>
      <div class="form-group">
        <label>Target:</label>
        <input v-model="targetUrl" placeholder="e.g. 8.8.8.8" />
      </div>
    </div>

    <section v-else class="monitor-section">
      <div class="panel graph-panel">
        <div class="card-head">
          <h3>Latency: {{ targetUrl }}</h3>
          <span class="pill soft-pill status-success">Live update</span>
        </div>
        <div class="canvas-wrapper">
          <canvas ref="chartCanvas" width="800" height="300"></canvas>
        </div>
        <div class="graph-legend">
          <span>Min: {{ minVal }}ms</span>
          <span>Max: {{ maxVal }}ms</span>
        </div>
      </div>

      <div class="panel table-panel">
        <h4>Recent Logs</h4>
        <table>
          <thead>
            <tr>
              <th>Time</th>
              <th>Value</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(log, i) in logs" :key="i">
              <td>{{ log.time }}</td>
              <td>{{ log.value }} ms</td>
              <td :class="log.status === 'Success' ? 'text-green' : 'text-red'">
                {{ log.status }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, onUnmounted, nextTick } from 'vue'
import axios from 'axios'

// State
const isMonitoring = ref(false)
const selectedAgentMode = ref('PING')
const targetUrl = ref('8.8.8.8')
const logs = ref([])
const dataPoints = ref(new Array(20).fill(0)) // กราฟเริ่มต้น
const timer = ref(null)
const chartCanvas = ref(null)
const maxVal = ref(100)
const minVal = ref(0)

// Toggle Function
const toggleMonitoring = () => {
  isMonitoring.value = !isMonitoring.value
  if (isMonitoring.value) {
    logs.value = []
    dataPoints.value = new Array(20).fill(0)
    fetchData() // ดึงครั้งแรก
    timer.value = setInterval(fetchData, 2000) // ดึงทุก 2 วิ
  } else {
    clearInterval(timer.value)
  }
}

// Fetch Data
const fetchData = async () => {
  try {
    // เรียก API
    const res = await axios.get(`http://localhost:5050/api/metrics/filter?target=${targetUrl.value}&type=${selectedAgentMode.value}`)
    
    // ข้อมูลจาก Backend เรียงจาก ใหม่ -> เก่า (ล่าสุดอยู่ตัวแรก)
    // เราเช็คก่อนว่ามีข้อมูลไหม
    if (res.data && res.data.length > 0) {
        const latestData = res.data[0] 
        const timeStr = new Date(latestData.timestamp).toLocaleTimeString()
        
        // ถ้าเป็นข้อมูลใหม่ (เทียบเวลา) หรือเป็นข้อมูลแรก
        if (logs.value.length === 0 || logs.value[0].timestamp !== latestData.timestamp) {
            
            // 1. เติม Log
            logs.value.unshift({
                time: timeStr,
                value: latestData.value,
                status: latestData.status,
                timestamp: latestData.timestamp
            })

            // 2. อัปเดตกราฟ (เอาตัวเก่าออกซ้ายสุด, เอาตัวใหม่เข้าขวาสุด)
            dataPoints.value.shift() 
            dataPoints.value.push(latestData.value)

            // 3. วาดกราฟ
            await nextTick()
            drawGraph()
        }
    }
  } catch (err) {
    console.error("Fetch error:", err)
  }
}

// Draw Graph (Canvas Logic)
const drawGraph = () => {
  const canvas = chartCanvas.value
  if (!canvas) return
  const ctx = canvas.getContext('2d')
  const width = canvas.width
  const height = canvas.height
  const data = dataPoints.value

  ctx.clearRect(0, 0, width, height)
  
  // คำนวณสเกลแกน Y (ให้กราฟยืดหยุ่นตามข้อมูลจริง)
  const max = Math.max(...data, 50) * 1.2
  maxVal.value = Math.round(max)

  ctx.beginPath()
  ctx.strokeStyle = '#00f2ff'
  ctx.lineWidth = 2
  
  const stepX = width / (data.length - 1)
  
  data.forEach((val, index) => {
    const x = index * stepX
    // กลับด้าน Y (ค่ามากอยู่บน)
    const y = height - (val / max) * height
    if (index === 0) ctx.moveTo(x, y)
    else ctx.lineTo(x, y)
  })
  
  ctx.stroke()
  
  // เพิ่มเงาใต้กราฟ (Optional)
  ctx.lineTo(width, height)
  ctx.lineTo(0, height)
  ctx.fillStyle = 'rgba(0, 242, 255, 0.1)'
  ctx.fill()
}

onUnmounted(() => clearInterval(timer.value))
</script>

<style scoped>
.page-shell { padding: 40px; color: white; }
.panel { background: #161b22; border: 1px solid #30363d; border-radius: 8px; padding: 20px; margin-bottom: 20px; }
.btn-primary { background: #238636; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; }
.btn-danger { background: #da3633; }
input, select { background: #0d1117; border: 1px solid #30363d; color: white; padding: 8px; width: 100%; margin-bottom: 10px; }
.canvas-wrapper { background: #0d1117; border: 1px solid #30363d; border-radius: 6px; padding: 10px; }
canvas { width: 100%; height: auto; display: block; }
table { width: 100%; margin-top: 10px; border-collapse: collapse; }
th, td { padding: 8px; border-bottom: 1px solid #30363d; text-align: left; }
.text-green { color: #3fb950; }
.text-red { color: #f85149; }
.graph-legend { display: flex; justify-content: space-between; font-size: 0.8em; color: #8b949e; margin-top: 5px; }
</style>