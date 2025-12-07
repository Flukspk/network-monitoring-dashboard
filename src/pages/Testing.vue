<template>
  <div class="page-shell">
    <header class="page-header">
      <div>
        <p class="eyebrow">Real-time Monitor</p>
        <h1>Agent Live Graph</h1>
        <p class="text-muted">Monitor latency from your 3 agents.</p>
      </div>
      <div class="header-actions">
        <button
          class="btn-primary"
          @click="toggleMonitoring"
          :class="{ 'btn-danger': isMonitoring }"
        >
          {{ isMonitoring ? "Stop Monitoring" : "Start Monitoring" }}
        </button>
      </div>
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
        <input
          v-model="targetUrl"
          placeholder="e.g. 8.8.8.8, 1.1.1.1, google.com, or https://www.github.com"
        />
        <div class="target-examples">
          <small
            >Examples:
            <span class="example-link" @click="targetUrl = '1.1.1.1'"
              >1.1.1.1</span
            >,
            <span class="example-link" @click="targetUrl = 'google.com'"
              >google.com</span
            >,
            <span
              class="example-link"
              @click="targetUrl = 'https://www.github.com'"
              >https://www.github.com</span
            >
          </small>
        </div>
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
import { ref, onUnmounted, nextTick } from "vue";
import axios from "axios";

// State
const isMonitoring = ref(false);
const selectedAgentMode = ref("PING");
const targetUrl = ref("");
const logs = ref([]);
const dataPoints = ref(Array.from({ length: 20 }, () => 0)); // กราฟเริ่มต้น
const timer = ref(null);
const chartCanvas = ref(null);
const maxVal = ref(100);
const minVal = ref(0);
const isRunningTest = ref(false);
// Use relative path for Docker (nginx proxy) or absolute for local dev
// In Docker, nginx proxies /api/* to backend, so use relative path
// For local dev, use the backend port directly
const getApiBaseUrl = () => {
  if (import.meta.env.VITE_API_URL) {
    return import.meta.env.VITE_API_URL;
  }
  // If running on port 8080 (Docker) or not localhost, use relative path
  if (
    window.location.port === "8080" ||
    window.location.hostname !== "localhost"
  ) {
    return "/api";
  }
  // Local dev - use direct backend port
  return "http://localhost:5050";
};
const API_BASE_URL = getApiBaseUrl();

// Toggle Function
const toggleMonitoring = () => {
  isMonitoring.value = !isMonitoring.value;
  if (isMonitoring.value) {
    // Check if target is provided
    if (!targetUrl.value.trim()) {
      alert("Please enter a target before starting monitoring");
      isMonitoring.value = false;
      return;
    }

    logs.value = [];
    dataPoints.value = Array.from({ length: 20 }, () => 0);

    // Run test immediately, then every 5 seconds
    runTestAndUpdate();
    timer.value = setInterval(runTestAndUpdate, 5000); // Run test every 5 seconds
  } else {
    clearInterval(timer.value);
    timer.value = null;
  }
};

// Run Test and Update Display
const runTestAndUpdate = async () => {
  if (!targetUrl.value.trim()) {
    console.warn("No target specified, skipping test");
    return;
  }

  isRunningTest.value = true;

  try {
    // เรียก API เพื่อรัน test
    const apiEndpoint = API_BASE_URL.startsWith("/")
      ? `${API_BASE_URL}/agent/run`
      : `${API_BASE_URL}/api/agent/run`;

    const res = await axios.post(apiEndpoint, {
      AgentId: selectedAgentMode.value,
      Target: targetUrl.value,
      MetricType: selectedAgentMode.value,
    });

    // Wait a moment for database to be ready, then fetch and update display
    setTimeout(() => {
      fetchData();
    }, 800);
  } catch (err) {
    console.error("Test error:", err);
    // Still try to fetch data in case previous tests succeeded
    setTimeout(() => {
      fetchData();
    }, 800);
  } finally {
    isRunningTest.value = false;
  }
};

// Fetch Data
const fetchData = async () => {
  try {
    // เรียก API
    // ถ้า API_BASE_URL เป็น /api แล้ว ไม่ต้องเพิ่ม /api อีก
    const apiEndpoint = API_BASE_URL.startsWith("/")
      ? `${API_BASE_URL}/metrics/filter?target=${encodeURIComponent(
          targetUrl.value
        )}&type=${selectedAgentMode.value}`
      : `${API_BASE_URL}/api/metrics/filter?target=${encodeURIComponent(
          targetUrl.value
        )}&type=${selectedAgentMode.value}`;

    console.log("Fetching data from:", apiEndpoint);
    const res = await axios.get(apiEndpoint);
    console.log("Fetched data:", res.data);

    // ข้อมูลจาก Backend เรียงจาก ใหม่ -> เก่า (ล่าสุดอยู่ตัวแรก)
    // เราเช็คก่อนว่ามีข้อมูลไหม
    if (res.data && res.data.length > 0) {
      const latestData = res.data[0];
      const timeStr = new Date(latestData.timestamp).toLocaleTimeString();

      // ถ้าเป็นข้อมูลใหม่ (เทียบเวลา) หรือเป็นข้อมูลแรก
      if (
        logs.value.length === 0 ||
        logs.value[0].timestamp !== latestData.timestamp
      ) {
        // 1. เติม Log
        logs.value.unshift({
          time: timeStr,
          value: latestData.value,
          status: latestData.status,
          timestamp: latestData.timestamp,
        });

        // 2. อัปเดตกราฟ (เอาตัวเก่าออกซ้ายสุด, เอาตัวใหม่เข้าขวาสุด)
        dataPoints.value.shift();
        dataPoints.value.push(latestData.value);

        // 3. วาดกราฟ
        await nextTick();
        drawGraph();
      }
    }
  } catch (err) {
    console.error("Fetch error:", err);
  }
};

// Draw Graph (Canvas Logic)
const drawGraph = () => {
  const canvas = chartCanvas.value;
  if (!canvas) return;
  const ctx = canvas.getContext("2d");
  const width = canvas.width;
  const height = canvas.height;
  const data = dataPoints.value;

  ctx.clearRect(0, 0, width, height);

  // คำนวณสเกลแกน Y (ให้กราฟยืดหยุ่นตามข้อมูลจริง)
  const max = Math.max(...data, 50) * 1.2;
  maxVal.value = Math.round(max);

  ctx.beginPath();
  ctx.strokeStyle = "#00f2ff";
  ctx.lineWidth = 2;

  const stepX = width / (data.length - 1);

  data.forEach((val, index) => {
    const x = index * stepX;
    // กลับด้าน Y (ค่ามากอยู่บน)
    const y = height - (val / max) * height;
    if (index === 0) ctx.moveTo(x, y);
    else ctx.lineTo(x, y);
  });

  ctx.stroke();

  // เพิ่มเงาใต้กราฟ (Optional)
  ctx.lineTo(width, height);
  ctx.lineTo(0, height);
  ctx.fillStyle = "rgba(0, 242, 255, 0.1)";
  ctx.fill();
};

onUnmounted(() => clearInterval(timer.value));
</script>

<style scoped>
.page-shell {
  padding: 40px;
  color: white;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}
.header-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}
.panel {
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 20px;
}
.btn-primary {
  background: #238636;
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s ease;
}
.btn-primary:hover:not(:disabled) {
  background: #2ea043;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(35, 134, 54, 0.3);
}
.run-test-btn {
  background: linear-gradient(120deg, #238636, #2ea043) !important;
}
.run-test-btn:hover:not(:disabled) {
  background: linear-gradient(120deg, #2ea043, #3fb950) !important;
  box-shadow: 0 6px 20px rgba(35, 134, 54, 0.4);
}
.btn-danger {
  background: #da3633;
}
input,
select {
  background: #0d1117;
  border: 1px solid #30363d;
  color: white;
  padding: 8px;
  width: 100%;
  margin-bottom: 10px;
  border-radius: 4px;
}
.form-group {
  margin-bottom: 16px;
}
.form-group label {
  display: block;
  margin-bottom: 6px;
  font-size: 14px;
  color: #9ba4c4;
}
.test-result {
  margin-top: 12px;
  padding: 10px;
  border-radius: 4px;
  font-size: 14px;
}
.test-result.success {
  background: rgba(65, 218, 187, 0.15);
  color: #41dabb;
  border: 1px solid rgba(65, 218, 187, 0.3);
}
.test-result.error {
  background: rgba(255, 107, 107, 0.15);
  color: #ff6b6b;
  border: 1px solid rgba(255, 107, 107, 0.3);
}
.test-result-panel {
  margin-bottom: 20px;
  padding: 16px;
}
.test-result-panel.success {
  background: rgba(65, 218, 187, 0.15);
  color: #41dabb;
  border: 1px solid rgba(65, 218, 187, 0.3);
}
.test-result-panel.error {
  background: rgba(255, 107, 107, 0.15);
  color: #ff6b6b;
  border: 1px solid rgba(255, 107, 107, 0.3);
}
.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.target-examples {
  margin-top: 8px;
  color: #9ba4c4;
  font-size: 12px;
}
.example-link {
  color: #4da5dd;
  cursor: pointer;
  text-decoration: underline;
  margin: 0 4px;
}
.example-link:hover {
  color: #62d6ff;
}
.canvas-wrapper {
  background: #0d1117;
  border: 1px solid #30363d;
  border-radius: 6px;
  padding: 10px;
}
canvas {
  width: 100%;
  height: auto;
  display: block;
}
table {
  width: 100%;
  margin-top: 10px;
  border-collapse: collapse;
}
th,
td {
  padding: 8px;
  border-bottom: 1px solid #30363d;
  text-align: left;
}
.text-green {
  color: #3fb950;
}
.text-red {
  color: #f85149;
}
.graph-legend {
  display: flex;
  justify-content: space-between;
  font-size: 0.8em;
  color: #8b949e;
  margin-top: 5px;
}
</style>
