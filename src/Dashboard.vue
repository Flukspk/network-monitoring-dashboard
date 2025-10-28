<template>
    <div class="dashboard-container">
      <Sidebar />
      <div class="content">
        <h1>Dashboard Overview</h1>
        <p>Monitor performance, equipment, and inventory across all departments</p>
  
        <div class="metrics">
          <div class="card">Latency<br /><span>120 ms</span></div>
          <div class="card">Packet Loss<br /><span>0.5%</span></div>
          <div class="card">Uptime<br /><span>99%</span></div>
          <div class="card">Resp. Time<br /><span>210 ms</span></div>
        </div>
  
        <div class="tables">
          <div class="table-box">
            <h3>Top 5 Slowest Target</h3>
          </div>
          <div class="map-box">
            <h3>Top 5 Location</h3>
          </div>
        </div>
      </div>
    </div>
  </template>
  
  <script setup>
  import Sidebar from "./Sidebar.vue";
  </script>
  
  <style scoped>
  .dashboard-container {
    display: flex;
    height: 100vh;
    background-color: #111;
    color: white;
  }
  
  .content {
    flex: 1;
    padding: 20px 40px;
    overflow-y: auto;
  }
  
  .metrics {
    display: flex;
    gap: 20px;
    margin-top: 20px;
  }
  
  .card {
    background-color: #1e1e1e;
    padding: 20px;
    border-radius: 10px;
    flex: 1;
    text-align: center;
    font-size: 18px;
  }
  
  .card span {
    display: block;
    font-size: 24px;
    margin-top: 10px;
  }
  
  .tables {
    display: flex;
    gap: 20px;
    margin-top: 30px;
  }
  .table-box, .map-box {
    flex: 1;
    background-color: #1e1e1e;
    border-radius: 10px;
    padding: 20px;
  }
  </style>
  