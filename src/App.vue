<template>
  <div class="login-page">
    <!-- top-left system name -->
    <div class="top-left-name">Network and Proactive Monitoring Agent</div>

    <!-- centered login block -->
    <section class="login-center">
      <LoginForm />
    </section>
  </div>
</template>

<script setup>
import LoginForm from "./components/LoginForm.vue";
</script>
