<template>
  <div class="login-card">
    <h1 class="login-title">Login</h1>

    <p class="login-subtitle">Welcome back! Please Login to your account</p>

    <form class="form-area" @submit.prevent="onSubmit">
      <!-- Email -->
      <div class="form-group">
        <label class="field-label" for="email">Email Address</label>
        <input
          id="email"
          v-model="email"
          class="input-box"
          type="email"
          placeholder="ABC123@gmail.com"
          required
        />
      </div>

      <!-- Password -->
      <div class="form-group">
        <label class="field-label" for="password">Password</label>
        <input
          id="password"
          v-model="password"
          class="input-box"
          type="password"
          placeholder="Enter Your Password"
          required
        />
      </div>

      <!-- Button -->
      <button class="login-btn" type="submit">Log In</button>
    </form>
  </div>
</template>

<script setup>
import { ref } from "vue";

const email = ref("");
const password = ref("");

function onSubmit() {
  console.log("login with", email.value, password.value);
  alert(`Logging in as ${email.value}`);
}
</script>
