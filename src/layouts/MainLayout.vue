<template>
  <div class="layout-shell">
    <Sidebar />
    <section class="layout-content">
      <router-view />
    </section>
  </div>
</template>

<script setup>
import Sidebar from "../Sidebar.vue";
</script>

<style scoped>
.layout-shell {
  display: flex;
  min-height: 100vh;
  color: var(--text-primary);
}

.layout-content {
  flex: 1;
  overflow-y: auto;
  background: transparent;
}
</style>

