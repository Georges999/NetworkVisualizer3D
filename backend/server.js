const express = require('express');
const cors = require('cors');
const http = require('http');
const socketIo = require('socket.io');
const ping = require('ping');
const arp = require('node-arp');
const si = require('systeminformation');
const os = require('os');

const app = express();
const server = http.createServer(app);
const io = socketIo(server, {
  cors: {
    origin: "http://localhost:3000",
    methods: ["GET", "POST"]
  }
});

app.use(cors());
app.use(express.json());

// Store discovered devices and connections
let devices = new Map();
let connections = new Map();
let networkStats = {
  totalDevices: 0,
  activeConnections: 0,
  packetsPerSecond: 0,
  bytesPerSecond: 0,
  protocolDistribution: {}
};

// Device types based on common patterns
const deviceTypeDetector = {
  detectType: (ip, hostname, mac) => {
    const h = hostname.toLowerCase();
    const macPrefix = mac.substring(0, 8).toUpperCase();
    
    // Common device type patterns
    if (h.includes('router') || h.includes('gateway')) return 'Router';
    if (h.includes('switch')) return 'Switch';
    if (h.includes('ap-') || h.includes('access-point')) return 'AccessPoint';
    if (h.includes('printer') || h.includes('hp-') || h.includes('canon-')) return 'Printer';
    if (h.includes('phone') || h.includes('android') || h.includes('iphone')) return 'MobilePhone';
    if (h.includes('tablet') || h.includes('ipad')) return 'Tablet';
    if (h.includes('server') || h.includes('srv-')) return 'Server';
    if (h.includes('camera') || h.includes('cam-')) return 'Camera';
    if (h.includes('tv') || h.includes('smart-tv')) return 'SmartTV';
    
    // MAC address based detection (common vendor prefixes)
    const vendorPatterns = {
      'Apple': ['00:03:93', '00:05:02', '00:0A:95', '00:0D:93'],
      'Samsung': ['00:07:AB', '00:12:FB', '00:15:99'],
      'Cisco': ['00:01:42', '00:01:43', '00:01:64'],
      'HP': ['00:01:E6', '00:02:A5', '00:04:EA']
    };
    
    for (const [vendor, prefixes] of Object.entries(vendorPatterns)) {
      if (prefixes.some(prefix => macPrefix.startsWith(prefix))) {
        if (vendor === 'Apple') return 'Computer';
        if (vendor === 'Cisco') return 'Router';
        if (vendor === 'HP') return 'Printer';
      }
    }
    
    return 'Computer'; // Default
  }
};

// Position calculator for 3D layout
const positionCalculator = {
  calculatePosition: (deviceCount, deviceType) => {
    const radius = 50 + (deviceCount * 5);
    const angle = (deviceCount * 137.5) * (Math.PI / 180); // Golden angle
    
    let y = 0;
    switch (deviceType) {
      case 'Router':
      case 'Gateway':
        y = 20;
        break;
      case 'Server':
        y = 15;
        break;
      case 'Switch':
        y = 10;
        break;
      case 'AccessPoint':
        y = 25;
        break;
      default:
        y = Math.random() * 10 - 5;
    }
    
    return {
      x: Math.cos(angle) * radius,
      y: y,
      z: Math.sin(angle) * radius
    };
  }
};

// Network discovery functions
async function discoverNetworkDevices() {
  try {
    const networkInterfaces = os.networkInterfaces();
    const localIPs = [];
    
    // Get local network ranges
    for (const [name, interfaces] of Object.entries(networkInterfaces)) {
      for (const iface of interfaces) {
        if (iface.family === 'IPv4' && !iface.internal) {
          localIPs.push(iface.address);
        }
      }
    }
    
    console.log('Scanning network ranges:', localIPs);
    
    for (const localIP of localIPs) {
      const subnet = localIP.substring(0, localIP.lastIndexOf('.'));
      await scanSubnet(subnet);
    }
    
  } catch (error) {
    console.error('Error discovering network devices:', error);
  }
}

async function scanSubnet(subnet) {
  const promises = [];
  
  // Scan common IP ranges (1-254)
  for (let i = 1; i <= 254; i++) {
    const ip = `${subnet}.${i}`;
    promises.push(checkDevice(ip));
    
    // Process in batches to avoid overwhelming the network
    if (i % 20 === 0) {
      await Promise.allSettled(promises.splice(0, 20));
      await new Promise(resolve => setTimeout(resolve, 100)); // Small delay
    }
  }
  
  // Process remaining promises
  if (promises.length > 0) {
    await Promise.allSettled(promises);
  }
}

async function checkDevice(ip) {
  try {
    const pingResult = await ping.promise.probe(ip, {
      timeout: 2,
      extra: ['-n', '1'] // Windows ping syntax
    });
    
    if (pingResult.alive) {
      await discoverDevice(ip, pingResult);
    }
  } catch (error) {
    // Ignore ping errors for non-responsive IPs
  }
}

async function discoverDevice(ip, pingResult) {
  try {
    // Get MAC address
    const macAddress = await new Promise((resolve) => {
      arp.getMAC(ip, (err, mac) => {
        resolve(mac || 'Unknown');
      });
    });
    
    // Try to get hostname
    let hostname = pingResult.host || ip;
    try {
      const dns = require('dns');
      const resolved = await new Promise((resolve) => {
        dns.reverse(ip, (err, hostnames) => {
          resolve(hostnames && hostnames.length > 0 ? hostnames[0] : ip);
        });
      });
      hostname = resolved;
    } catch (e) {
      // Use IP if hostname resolution fails
    }
    
    const deviceType = deviceTypeDetector.detectType(ip, hostname, macAddress);
    const position = positionCalculator.calculatePosition(devices.size, deviceType);
    
    const device = {
      id: ip,
      ipAddress: ip,
      macAddress: macAddress,
      hostname: hostname,
      deviceType: deviceType,
      position: position,
      lastSeen: new Date(),
      firstSeen: devices.has(ip) ? devices.get(ip).firstSeen : new Date(),
      isOnline: true,
      responseTime: pingResult.time || 0,
      vendor: 'Unknown'
    };
    
    const isNewDevice = !devices.has(ip);
    devices.set(ip, device);
    
    if (isNewDevice) {
      console.log(`Discovered new device: ${ip} (${hostname}) - ${deviceType}`);
      io.emit('deviceDiscovered', device);
    } else {
      io.emit('deviceUpdated', device);
    }
    
    // Simulate connection discovery based on device types
    await simulateConnections(device);
    
  } catch (error) {
    console.error(`Error discovering device ${ip}:`, error);
  }
}

async function simulateConnections(device) {
  // Create connections between devices based on realistic network patterns
  const existingDevices = Array.from(devices.values());
  
  // Routers typically connect to many devices
  if (device.deviceType === 'Router') {
    existingDevices.forEach(otherDevice => {
      if (otherDevice.id !== device.id && otherDevice.deviceType !== 'Router') {
        createConnection(device, otherDevice, 'Gateway');
      }
    });
  }
  
  // Servers often have connections to multiple clients
  if (device.deviceType === 'Server') {
    const clients = existingDevices.filter(d => 
      d.deviceType === 'Computer' || d.deviceType === 'MobilePhone'
    );
    clients.slice(0, 5).forEach(client => {
      createConnection(device, client, 'Service');
    });
  }
  
  // Regular devices connect to routers/gateways
  if (['Computer', 'MobilePhone', 'Tablet'].includes(device.deviceType)) {
    const routers = existingDevices.filter(d => d.deviceType === 'Router');
    if (routers.length > 0) {
      createConnection(device, routers[0], 'Internet');
    }
  }
}

function createConnection(sourceDevice, targetDevice, connectionType) {
  const connectionId = `${sourceDevice.id}-${targetDevice.id}`;
  
  if (!connections.has(connectionId)) {
    const connection = {
      id: connectionId,
      sourceIp: sourceDevice.ipAddress,
      destinationIp: targetDevice.ipAddress,
      sourceDevice: sourceDevice,
      targetDevice: targetDevice,
      connectionType: connectionType,
      protocol: 'TCP',
      state: 'Established',
      startTime: new Date(),
      lastActivity: new Date(),
      bytesTransferred: Math.floor(Math.random() * 1000000),
      packetsCount: Math.floor(Math.random() * 1000),
      isActive: true
    };
    
    connections.set(connectionId, connection);
    io.emit('connectionEstablished', connection);
  }
}

// Update network statistics
function updateNetworkStats() {
  networkStats = {
    totalDevices: devices.size,
    activeConnections: connections.size,
    packetsPerSecond: Math.floor(Math.random() * 100) + 50,
    bytesPerSecond: Math.floor(Math.random() * 1000000) + 100000,
    protocolDistribution: {
      'TCP': 60 + Math.random() * 20,
      'UDP': 20 + Math.random() * 15,
      'ICMP': 5 + Math.random() * 10,
      'HTTP': 10 + Math.random() * 15,
      'HTTPS': 5 + Math.random() * 10
    }
  };
  
  io.emit('networkStats', networkStats);
}

// API Routes
app.get('/api/devices', (req, res) => {
  res.json(Array.from(devices.values()));
});

app.get('/api/connections', (req, res) => {
  res.json(Array.from(connections.values()));
});

app.get('/api/stats', (req, res) => {
  res.json(networkStats);
});

app.post('/api/scan', async (req, res) => {
  console.log('Starting network scan...');
  discoverNetworkDevices();
  res.json({ message: 'Network scan started' });
});

// Socket.IO connection handling
io.on('connection', (socket) => {
  console.log('Client connected:', socket.id);
  
  // Send current data to new client
  socket.emit('initialData', {
    devices: Array.from(devices.values()),
    connections: Array.from(connections.values()),
    stats: networkStats
  });
  
  socket.on('startScan', () => {
    console.log('Client requested network scan');
    discoverNetworkDevices();
  });
  
  socket.on('disconnect', () => {
    console.log('Client disconnected:', socket.id);
  });
});

// Start periodic updates
setInterval(updateNetworkStats, 2000);
setInterval(() => {
  // Periodic device health check
  devices.forEach(async (device) => {
    try {
      const pingResult = await ping.promise.probe(device.ipAddress, {
        timeout: 1,
        extra: ['-n', '1']
      });
      
      device.isOnline = pingResult.alive;
      device.responseTime = pingResult.time || 0;
      device.lastSeen = pingResult.alive ? new Date() : device.lastSeen;
      
      io.emit('deviceUpdated', device);
    } catch (error) {
      device.isOnline = false;
      io.emit('deviceUpdated', device);
    }
  });
}, 30000); // Check every 30 seconds

const PORT = process.env.PORT || 5000;
server.listen(PORT, () => {
  console.log(`NetworkVisualizer3D Backend running on port ${PORT}`);
  console.log('Starting initial network discovery...');
  
  // Start initial scan after a short delay
  setTimeout(() => {
    discoverNetworkDevices();
  }, 2000);
}); 