<template>
  <div class="page-shell">
    <header class="page-header">
      <div>
        <p class="eyebrow">Directory</p>
        <h1>User access</h1>
        <p class="text-muted">
          Overview of all accounts that can access the monitoring workspace. Use this list to keep
          credentials in sync with the backend.
        </p>
      </div>
      <div class="header-actions">
        <button class="btn-ghost">Export CSV</button>
        <button class="btn-primary">Invite user</button>
      </div>
    </header>

    <section class="panel users-table">
      <table>
        <thead>
          <tr>
            <th>User</th>
            <th>Email</th>
            <th>Role</th>
            <th>Created</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="user in users" :key="user.email">
            <td class="user-cell">
              <div class="avatar">{{ initials(user.name) }}</div>
              <div>
                <p class="user-name">{{ user.name }}</p>
                <p class="text-muted">{{ user.handle }}</p>
              </div>
            </td>
            <td>{{ user.email }}</td>
            <td>{{ user.role }}</td>
            <td>{{ user.created }}</td>
            <td>
              <span class="pill soft-pill">
                <span class="status-dot" :class="user.statusClass"></span>
                {{ user.status }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </section>
  </div>
</template>

<script setup>
const users = [
  {
    name: "Fluk Narong",
    handle: "@fluk",
    email: "fluk@example.com",
    role: "Administrator",
    created: "Oct 28, 2025",
    status: "Active",
    statusClass: "status-success",
  },
  {
    name: "Intouch P.",
    handle: "@intouch",
    email: "Intouch@gmail.com",
    role: "Operator",
    created: "Nov 24, 2025",
    status: "Pending reset",
    statusClass: "status-warning",
  },
  {
    name: "Ops Bot",
    handle: "@automation",
    email: "bot@network-monitoring.local",
    role: "Service",
    created: "Oct 12, 2025",
    status: "Active",
    statusClass: "status-success",
  },
];

const initials = (name) =>
  name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
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
  gap: 24px;
}

.header-actions {
  display: flex;
  gap: 12px;
}

h1 {
  margin: 6px 0;
  font-size: clamp(28px, 4vw, 36px);
}

.users-table table {
  width: 100%;
  border-collapse: collapse;
}

th {
  text-align: left;
  font-size: 12px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--text-muted);
  padding-bottom: 12px;
}

td {
  padding: 16px 0;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
}

.user-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.avatar {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.08);
  display: grid;
  place-items: center;
  font-weight: 600;
}

.user-name {
  font-weight: 600;
}

@media (max-width: 768px) {
  .page-header {
    flex-direction: column;
  }

  .users-table {
    overflow-x: auto;
  }

  table {
    min-width: 640px;
  }
}
</style>

