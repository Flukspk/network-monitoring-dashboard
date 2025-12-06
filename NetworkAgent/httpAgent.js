const axios = require('axios');

async function runHttpCheck(url) {
  const start = Date.now();
  try {
    const res = await axios.get(url, { timeout: 5000 });
    const ms = Date.now() - start;
    return {
      Target: url,
      TargetType: "http",
      LatencyMs: ms,
      PacketLoss: res.status >= 200 && res.status < 400 ? 0 : 100,
      ResponseTimeMs: ms,
      Timestamp: new Date().toISOString()
    };
  } catch (err) {
    return {
      Target: url,
      TargetType: 'http',
      LatencyMs: -1,
      PacketLoss: 100,
      ResponseTimeMs: -1,
      Timestamp: new Date().toISOString()
    };
  }
}

module.exports = { runHttpCheck };
