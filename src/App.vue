<template>
  <div class="app">
    <!-- top-left system name -->
    <div class="top-left-name"></div>

    <!-- router-view แสดงหน้าตาม route -->
    <router-view />
  </div>
</template>

<script setup>
</script>

<style scoped>
.app {
  height: 100vh;
  width: 100vw;
  position: relative;
  background-color: #111; 
  color: white;
  font-family: sans-serif;
}

.top-left-name {
  position: absolute;
  top: 20px;
  left: 20px;
  font-size: 18px;
  font-weight: bold;
}
</style>
