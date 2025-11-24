<template>
  <div class="page-shell">
    <header class="page-header">
      <div>
        <p class="eyebrow">Lab testing</p>
        <h1>Scenario validation</h1>
        <p class="text-muted">
          Run synthetic flows and capture baselines before promoting config changes to the live
          dashboard.
        </p>
      </div>
      <button class="btn-primary">New test run</button>
    </header>

    <section class="grid-auto-fit">
      <article class="panel test-card" v-for="suite in testSuites" :key="suite.name">
        <div class="card-head">
          <h3>{{ suite.name }}</h3>
          <span class="pill soft-pill">
            <span class="status-dot" :class="suite.statusClass"></span>
            {{ suite.status }}
          </span>
        </div>
        <p class="text-muted">{{ suite.description }}</p>
        <div class="card-footer">
          <span>Last run · {{ suite.lastRun }}</span>
          <button class="btn-ghost compact">View log</button>
        </div>
      </article>
    </section>
  </div>
</template>

<script setup>
const testSuites = [
  {
    name: "Failover latency",
    description: "Validate BGP failover within acceptable packet loss thresholds.",
    status: "Passing",
    statusClass: "status-success",
    lastRun: "5 minutes ago",
  },
  {
    name: "HTTP path validation",
    description: "Synthetic transactions for public APIs with TLS renegotiation.",
    status: "Investigate",
    statusClass: "status-warning",
    lastRun: "18 minutes ago",
  },
  {
    name: "VoIP jitter",
    description: "SIP trunk jitter and MOS calculation for APAC routes.",
    status: "Passing",
    statusClass: "status-success",
    lastRun: "1 hour ago",
  },
];
</script>

<style scoped>
.page-shell {
  padding: 48px clamp(24px, 6vw, 72px);
  display: flex;
  flex-direction: column;
  gap: 28px;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 20px;
}

h1 {
  margin: 6px 0;
  font-size: clamp(28px, 4vw, 36px);
}

.test-card {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.card-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 14px;
  color: var(--text-muted);
}

@media (max-width: 768px) {
  .page-header {
    flex-direction: column;
  }
}
</style>

