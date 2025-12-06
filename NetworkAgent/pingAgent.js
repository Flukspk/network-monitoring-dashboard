const ping = require('ping');
const axios = require('axios');
const fs = require('fs');

const cfg = JSON.parse(fs.readFileSync('config.json'));

async function runPing(target) {
  const count = 4;
  let received = 0;
  let totalMs = 0;

  for (let i=0;i<count;i++) {
    const res = await ping.promise.probe(target, { timeout: 2 });
    if (res.alive) {
      received++;
      // res.time is a string millisecond or 'unknown'
      const t = parseFloat(res.time) || 0;
      totalMs += t;
    }
  }
  return {
    Target: target,
    TargetType: "ping",
    LatencyMs: received ? totalMs/received : -1,
    PacketLoss: ((count - received) / count) * 100,
    ResponseTimeMs: received ? totalMs/received : -1,
    Timestamp: new Date().toISOString()
  };
}

async function sendMetric(metric) {
  try {
    await axios.post(cfg.backendUrl, metric, { timeout: 5000 });
    console.log('sent', metric.TargetType, metric.Target);
  } catch (err) {
    console.error('failed send', err.message);
  }
}

module.exports = { runPing, sendMetric };
