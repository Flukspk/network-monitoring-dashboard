import ping from 'ping';
import fs from 'fs';
import axios from 'axios';
import config from './config.json' assert { type: "json" };

async function runPingTests() {
  const results = [];
  for (const target of config.pingTargets) {
    const res = await ping.promise.probe(target);
    results.push({
      target,
      alive: res.alive,
      time: res.time,
      timestamp: new Date()
    });
  }

  // ส่งข้อมูลไป backend
  await axios.post(`${config.backendUrl}/api/metrics/ping`, results);
}

setInterval(runPingTests, config.intervalMs);
