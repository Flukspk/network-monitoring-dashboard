const traceroute = require('traceroute-lite');

async function runTraceroute(host) {
  return new Promise((resolve) => {
    const hops = [];
    traceroute.trace(host, (err, hop) => {
      if (err) {
        resolve({ Target: host, TargetType: 'traceroute', Hops: [], Timestamp: new Date().toISOString() });
      } else {
        if (hop) hops.push(hop);
        else resolve({ Target: host, TargetType: 'traceroute', Hops: hops, Timestamp: new Date().toISOString() });
      }
    });
  });
}

module.exports = { runTraceroute };
