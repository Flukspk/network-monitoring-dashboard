<template>
    <div class="sidebar">
      <div class="logo">Dashboard</div>
      <ul class="menu">
        <li><router-link to="/dashboard">🏠 Dashboard</router-link></li>
        <li><router-link to="/test">📊 Test</router-link></li>
        <li><router-link to="/event">📅 Event</router-link></li>
        <li><router-link to="/alert">🚨 Alert</router-link></li>
        <li><router-link to="/user">👤 User</router-link></li>
      </ul>
      <button class="logout-btn" @click="logout">Logout</button>
    </div>
  </template>
  
  <script setup>
  import { useRouter } from "vue-router";
  const router = useRouter();
  
  function logout() {
    localStorage.removeItem("user");
    router.push("/login");
  }
  </script>
  
  <style scoped>
  .sidebar {
    width: 220px;
    height: 100vh;
    background-color: #1e1e1e;
    color: #fff;
    display: flex;
    flex-direction: column;
    padding: 20px;
  }
  
  .logo {
    font-size: 18px;
    font-weight: bold;
    margin-bottom: 30px;
  }
  
  .menu {
    list-style: none;
    padding: 0;
    flex-grow: 1;
  }
  
  .menu li {
    margin: 15px 0;
  }
  
  .menu a {
    color: white;
    text-decoration: none;
  }
  
  .menu a.router-link-active {
    color: #00bcd4;
    font-weight: bold;
  }
  
  .logout-btn {
    background: #ff5252;
    border: none;
    padding: 10px;
    color: white;
    cursor: pointer;
    border-radius: 6px;
  }
  </style>
  