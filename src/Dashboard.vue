<template>
  <main class="workspace">
    <header class="workspace-header">
      <div class="header-info">
        <p class="eyebrow">Senior project</p>
        <div class="title-row">
          <h1>Network Monitoring Dashboard</h1>
          <span class="pill header-pill">
            <span class="status-dot status-success"></span>
            Stable
          </span>
        </div>
        <p class="text-muted">
          Demonstration view of latency, uptime, and probe health for the senior
          project network.
        </p>
      </div>
      <div class="header-actions">
        <button class="btn-ghost">Download report</button>
        <button class="btn-primary">Add widget</button>
      </div>
    </header>

    <section class="metrics-grid">
      <article
        v-for="metric in metrics"
        :key="metric.label"
        class="metric-card panel"
      >
        <div class="metric-head">
          <p class="metric-label">{{ metric.label }}</p>
          <span class="trend-pill" :class="metric.trend">
            {{ metric.trend === "up" ? "↑" : "↓" }} {{ metric.delta }}
          </span>
        </div>
        <h2>{{ metric.value }}</h2>
        <p class="metric-subtitle">{{ metric.caption }}</p>
        <div class="progress-track">
          <div
            class="progress-fill"
            :style="{ width: metric.progress + '%' }"
          ></div>
        </div>
        <div class="metric-footer">
          <span>{{ metric.baseline }}</span>
          <span>{{ metric.target }}</span>
        </div>
      </article>
    </section>

    <section class="panels-grid">
      <article class="panel wide-panel">
        <div class="panel-header">
          <div>
            <p class="panel-title">Probe health</p>
            <p class="panel-subtitle">Edge agents & regional sensors</p>
          </div>
          <button class="btn-ghost compact">View agents</button>
        </div>
        <div class="probe-grid">
          <div
            v-for="probe in probeHealth"
            :key="probe.name"
            class="probe-card"
          >
            <div class="probe-head">
              <span class="probe-name">{{ probe.name }}</span>
              <span class="pill soft-pill">
                <span class="status-dot" :class="probe.statusClass"></span>
                {{ probe.status }}
              </span>
            </div>
            <p class="probe-meta">{{ probe.region }}</p>
            <div class="probe-metrics">
              <div>
                <p class="probe-label">Latency</p>
                <p class="probe-value">{{ probe.latency }}</p>
              </div>
              <div>
                <p class="probe-label">Packet loss</p>
                <p class="probe-value">{{ probe.loss }}</p>
              </div>
              <div>
                <p class="probe-label">Uptime</p>
                <p class="probe-value">{{ probe.uptime }}</p>
              </div>
            </div>
          </div>
        </div>
      </article>

      <article class="panel table-panel">
        <div class="panel-header">
          <div>
            <p class="panel-title">Slowest targets</p>
            <p class="panel-subtitle">Monitored last 15 minutes</p>
          </div>
          <button class="btn-ghost compact">View all</button>
        </div>
        <table>
          <thead>
            <tr>
              <th>Target</th>
              <th>Location</th>
              <th>Latency</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="target in topSlowTargets" :key="target.name">
              <td>{{ target.name }}</td>
              <td>{{ target.location }}</td>
              <td>{{ target.latency }}</td>
              <td>
                <span class="pill soft-pill">
                  <span class="status-dot" :class="target.statusClass"></span>
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
            <p class="panel-title">Top availability</p>
            <p class="panel-subtitle">Best performing regions</p>
          </div>
          <button class="btn-ghost compact">Share</button>
        </div>
        <ul class="location-list">
          <li v-for="location in topLocations" :key="location.name">
            <div>
              <p class="location-name">{{ location.name }}</p>
              <p class="location-meta text-muted">{{ location.providers }}</p>
            </div>
            <div class="location-score">
              <strong>{{ location.availability }}</strong>
              <span>{{ location.latency }}</span>
            </div>
          </li>
        </ul>
      </article>
    </section>
  </main>
</template>

<script setup>
const metrics = [
  {
    label: "Mean latency",
    value: "118 ms",
    caption: "Global median",
    delta: "12 ms",
    trend: "down",
    progress: 62,
    baseline: "Last hour 130 ms",
    target: "Target 90 ms",
  },
  {
    label: "Packet loss",
    value: "0.41%",
    caption: "Across ICMP & HTTP probes",
    delta: "0.1%",
    trend: "down",
    progress: 38,
    baseline: "Prev. 0.51%",
    target: "Target 0.20%",
  },
  {
    label: "Availability",
    value: "99.3%",
    caption: "Weighted SLA",
    delta: "0.3%",
    trend: "up",
    progress: 82,
    baseline: "Prev. 99.0%",
    target: "Goal 99.9%",
  },
  {
    label: "Median response",
    value: "204 ms",
    caption: "HTTP GET time",
    delta: "18 ms",
    trend: "up",
    progress: 54,
    baseline: "Prev. 186 ms",
    target: "Goal 150 ms",
  },
];

const probeHealth = [
  {
    name: "Core edge agent",
    region: "Global",
    status: "Healthy",
    statusClass: "status-success",
    latency: "112 ms",
    loss: "0.28%",
    uptime: "99.9%",
  },
  {
    name: "APAC sensor",
    region: "Singapore",
    status: "Degraded",
    statusClass: "status-warning",
    latency: "182 ms",
    loss: "0.67%",
    uptime: "99.2%",
  },
  {
    name: "EU backbone",
    region: "Frankfurt",
    status: "Healthy",
    statusClass: "status-success",
    latency: "96 ms",
    loss: "0.19%",
    uptime: "99.8%",
  },
];

const topSlowTargets = [
  {
    name: "Core Router 01",
    location: "Singapore",
    latency: "212 ms",
    status: "Investigate",
    statusClass: "status-warning",
  },
  {
    name: "Inventory API",
    location: "Chicago",
    latency: "198 ms",
    status: "Watching",
    statusClass: "status-warning",
  },
  {
    name: "VPN Gateway",
    location: "São Paulo",
    latency: "184 ms",
    status: "Healthy",
    statusClass: "status-success",
  },
  {
    name: "DNS Resolver",
    location: "Sydney",
    latency: "176 ms",
    status: "Healthy",
    statusClass: "status-success",
  },
  {
    name: "Edge POP West",
    location: "Los Angeles",
    latency: "169 ms",
    status: "Alerted",
    statusClass: "status-danger",
  },
];

const topLocations = [
  {
    name: "London, UK",
    providers: "BT, Vodafone, Colt",
    availability: "99.8%",
    latency: "98 ms",
  },
  {
    name: "Jakarta, ID",
    providers: "Biznet, Telkomsel",
    availability: "99.4%",
    latency: "142 ms",
  },
  {
    name: "Dallas, US",
    providers: "Lumen, AT&T",
    availability: "99.2%",
    latency: "108 ms",
  },
  {
    name: "Sydney, AU",
    providers: "Telstra, Optus",
    availability: "99.0%",
    latency: "156 ms",
  },
];
</script>

<style scoped>
.workspace {
  padding: 40px clamp(24px, 4vw, 60px);
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
  color: var(--text-muted);
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

.header-actions {
  display: flex;
  gap: 12px;
  align-items: center;
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
}

.metric-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.metric-label {
  font-size: 13px;
  text-transform: uppercase;
  letter-spacing: 0.18em;
  color: var(--text-muted);
}

.metric-card h2 {
  font-size: 32px;
  font-weight: 600;
}

.metric-subtitle {
  color: var(--text-muted);
}

.trend-pill {
  border-radius: 999px;
  padding: 4px 12px;
  font-size: 13px;
}

.trend-pill.up {
  background: rgba(65, 218, 187, 0.12);
  color: var(--success);
}

.trend-pill.down {
  background: rgba(255, 107, 107, 0.12);
  color: var(--danger);
}

.progress-track {
  width: 100%;
  height: 6px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.06);
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  border-radius: inherit;
  background: linear-gradient(120deg, var(--accent), var(--accent-strong));
}

.metric-footer {
  font-size: 12px;
  display: flex;
  justify-content: space-between;
  color: var(--text-muted);
}

.panels-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 24px;
  margin-top: 32px;
}

.wide-panel {
  grid-column: span 2;
}

.probe-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
}

.probe-card {
  border: 1px solid rgba(255, 255, 255, 0.05);
  border-radius: var(--radius-lg);
  padding: 18px;
  background: rgba(255, 255, 255, 0.02);
}

.probe-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.probe-name {
  font-weight: 600;
}

.probe-meta {
  color: var(--text-muted);
  font-size: 14px;
}

.probe-metrics {
  display: flex;
  justify-content: space-between;
  margin-top: 12px;
}

.probe-label {
  font-size: 12px;
  text-transform: uppercase;
  color: var(--text-muted);
  letter-spacing: 0.16em;
}

.probe-value {
  font-size: 16px;
  font-weight: 600;
}

.table-panel table {
  width: 100%;
  border-collapse: collapse;
}

th,
td {
  padding: 12px 0;
  text-align: left;
}

th {
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.14em;
  color: var(--text-muted);
}

tbody tr + tr {
  border-top: 1px solid rgba(255, 255, 255, 0.05);
}

.location-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.location-list li {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
}

.location-list li:last-child {
  border-bottom: none;
}

.location-name {
  font-weight: 600;
}

.location-score {
  text-align: right;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.location-score strong {
  font-size: 18px;
}

.header-pill {
  background: rgba(77, 165, 221, 0.15);
  border-color: rgba(77, 165, 221, 0.35);
}

.compact {
  padding: 8px 14px;
  font-size: 13px;
}

@media (max-width: 960px) {
  .workspace-header {
    flex-direction: column;
  }

  .header-actions {
    width: 100%;
    justify-content: flex-start;
  }

  .wide-panel {
    grid-column: span 1;
  }
}
</style>
