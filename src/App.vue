<template>
  <div class="app-shell">
    <div class="app-backdrop" aria-hidden="true">
      <div class="orb orb-one"></div>
      <div class="orb orb-two"></div>
      <div class="grain"></div>
    </div>
    <div class="app-content">
      <router-view />
    </div>
  </div>
</template>

<script setup></script>

<style scoped>
.app-shell {
  min-height: 100vh;
  width: 100%;
  position: relative;
  overflow: hidden;
  background: radial-gradient(circle at 20% 20%, rgba(77, 165, 221, 0.08), transparent 55%),
    radial-gradient(circle at 80% 0%, rgba(111, 80, 220, 0.1), transparent 45%),
    #050608;
}

.app-backdrop {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.app-content {
  position: relative;
  z-index: 2;
  min-height: 100vh;
}

.orb {
  position: absolute;
  width: 320px;
  height: 320px;
  border-radius: 50%;
  filter: blur(60px);
  opacity: 0.45;
}

.orb-one {
  top: -60px;
  left: -40px;
  background: #4da5dd;
}

.orb-two {
  right: -120px;
  bottom: -40px;
  background: #924dff;
}

.grain {
  position: absolute;
  inset: 0;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='160' height='160' viewBox='0 0 160 160'%3E%3Cfilter id='noise' x='0%25' y='0%25' width='100%25' height='100%25'%3E%3CfeTurbulence baseFrequency='0.65' numOctaves='3' in='SourceGraphic'%3E%3C/feTurbulence%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23noise)' opacity='0.08'%3E%3C/rect%3E%3C/svg%3E");
  opacity: 0.35;
  mix-blend-mode: screen;
}
</style>
