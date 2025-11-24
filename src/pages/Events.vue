<template>
  <div class="page-shell">
    <header class="page-header">
      <div>
        <p class="eyebrow">Timeline</p>
        <h1>Network events</h1>
        <p class="text-muted">
          Track change windows, maintenance notes, and detected anomalies affecting performance.
        </p>
      </div>
      <button class="btn-primary">Add event</button>
    </header>

    <section class="event-list panel">
      <article v-for="event in events" :key="event.id" class="event-row">
        <div class="event-time">
          <strong>{{ event.time }}</strong>
          <span>{{ event.date }}</span>
        </div>
        <div class="event-body">
          <div class="event-title">
            <h3>{{ event.title }}</h3>
            <span class="pill soft-pill">
              <span class="status-dot" :class="event.statusClass"></span>
              {{ event.category }}
            </span>
          </div>
          <p class="text-muted">{{ event.detail }}</p>
        </div>
      </article>
    </section>
  </div>
</template>

<script setup>
const events = [
  {
    id: 1,
    time: "09:45",
    date: "Nov 24",
    title: "Planned fiber maintenance",
    category: "Maintenance",
    statusClass: "status-warning",
    detail: "Metro fiber vendor performing splice work between core routers.",
  },
  {
    id: 2,
    time: "11:10",
    date: "Nov 24",
    title: "Config deploy window",
    category: "Change",
    statusClass: "status-success",
    detail: "Rolling out updated QoS policies to the APAC edge routers.",
  },
  {
    id: 3,
    time: "13:20",
    date: "Nov 24",
    title: "High latency detected",
    category: "Incident",
    statusClass: "status-danger",
    detail: "Latency spike on Singapore probe, linked to upstream congestion.",
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

.event-list {
  display: flex;
  flex-direction: column;
  padding: 0;
}

.event-row {
  display: grid;
  grid-template-columns: 140px 1fr;
  gap: 24px;
  padding: 24px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
}

.event-row:last-child {
  border-bottom: none;
}

.event-time strong {
  display: block;
  font-size: 24px;
}

.event-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
}

@media (max-width: 768px) {
  .page-header {
    flex-direction: column;
  }

  .event-row {
    grid-template-columns: 1fr;
  }

  .event-title {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>

