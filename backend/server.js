const express = require('express');
const cors = require('cors');
const http = require('http');
const socketIo = require('socket.io');
const ping = require('ping');
const arp = require('node-arp');
const si = require('systeminformation');
const os = require('os');
const dns = require('dns');
const { exec } = require('child_process');
const util = require('util');
const execPromise = util.promisify(exec);

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

// Enhanced device type detector with more patterns
const deviceTypeDetector = {
  detectType: (ip, hostname, mac, openPorts = []) => {
    const h = hostname.toLowerCase();
    const macPrefix = mac.substring(0, 8).toUpperCase();
    
    // Hostname-based detection
    if (h.includes('router') || h.includes('gateway') || h.includes('.router')) return 'Router';
    if (h.includes('switch')) return 'Switch';
    if (h.includes('ap-') || h.includes('access-point') || h.includes('wap')) return 'AccessPoint';
    if (h.includes('printer') || h.includes('hp') || h.includes('canon') || h.includes('epson')) return 'Printer';
    if (h.includes('phone') || h.includes('android') || h.includes('iphone') || h.includes('mobile')) return 'MobilePhone';
    if (h.includes('tablet') || h.includes('ipad')) return 'Tablet';
    if (h.includes('server') || h.includes('srv') || h.includes('dc-')) return 'Server';
    if (h.includes('camera') || h.includes('cam') || h.includes('ipcam')) return 'Camera';
    if (h.includes('tv') || h.includes('smart-tv') || h.includes('roku') || h.includes('chromecast')) return 'SmartTV';
    if (h.includes('nas') || h.includes('synology') || h.includes('qnap')) return 'Server';
    
    // Port-based detection
    if (openPorts.includes(80) || openPorts.includes(443)) {
      if (openPorts.includes(22) || openPorts.includes(3389)) return 'Server';
      if (openPorts.includes(9100) || openPorts.includes(631)) return 'Printer';
    }
    if (openPorts.includes(554) || openPorts.includes(8080)) return 'Camera';
    if (openPorts.includes(445) || openPorts.includes(139)) return 'Computer';
    
    // MAC address vendor-based detection
    const vendorPatterns = {
      'Apple': {
        prefixes: ['00:03:93', '00:05:02', '00:0A:95', '00:0D:93', '00:16:CB', '00:17:F2', '00:19:E3', '00:1B:63', '00:1C:B3'],
        types: ['Computer', 'MobilePhone', 'Tablet']
      },
      'Samsung': {
        prefixes: ['00:07:AB', '00:12:FB', '00:15:99', '00:16:32', '00:16:6C', '00:16:DB'],
        types: ['MobilePhone', 'Tablet', 'SmartTV']
      },
      'Cisco': {
        prefixes: ['00:01:42', '00:01:43', '00:01:64', '00:01:96', '00:01:97'],
        types: ['Router', 'Switch', 'AccessPoint']
      },
      'HP': {
        prefixes: ['00:01:E6', '00:02:A5', '00:04:EA', '00:08:02', '00:0D:9D'],
        types: ['Printer', 'Computer']
      },
      'Canon': {
        prefixes: ['00:00:85', '00:1E:8F', '18:0C:AC'],
        types: ['Printer']
      },
      'Netgear': {
        prefixes: ['00:09:5B', '00:0F:B5', '00:14:6C', '00:1B:2F'],
        types: ['Router', 'AccessPoint']
      },
      'Raspberry': {
        prefixes: ['B8:27:EB', 'DC:A6:32', 'E4:5F:01'],
        types: ['Computer', 'Server']
      }
    };
    
    for (const [vendor, data] of Object.entries(vendorPatterns)) {
      if (data.prefixes.some(prefix => macPrefix.startsWith(prefix))) {
        // Return the most likely type for this vendor
        return data.types[0];
      }
    }
    
    // Default based on response characteristics
    if (openPorts.length > 5) return 'Server';
    return 'Computer';
  },
  
  getVendorFromMac: (mac) => {
    const macPrefix = mac.substring(0, 8).toUpperCase();
    const vendorMap = {
      '00:03:93': 'Apple Inc.',
      '00:05:02': 'Apple Inc.',
      '00:07:AB': 'Samsung Electronics',
      '00:12:FB': 'Samsung Electronics',
      '00:01:42': 'Cisco Systems',
      '00:01:E6': 'Hewlett Packard',
      '00:00:85': 'Canon Inc.',
      '00:09:5B': 'Netgear',
      'B8:27:EB': 'Raspberry Pi Foundation',
      '00:50:56': 'VMware, Inc.',
      '00:15:5D': 'Microsoft Corp.',
      '00:1A:7D': 'D-Link',
      '00:24:01': 'D-Link',
      '00:04:4B': 'NVIDIA',
      '00:E0:4C': 'Realtek',
      '00:25:22': 'ASRock',
    };
    
    for (const [prefix, vendor] of Object.entries(vendorMap)) {
      if (macPrefix.startsWith(prefix)) {
        return vendor;
      }
    }
    
    return 'Unknown';
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

// Enhanced port scanner
async function scanCommonPorts(ip) {
  const commonPorts = [
    { port: 22, service: 'SSH' },
    { port: 23, service: 'Telnet' },
    { port: 80, service: 'HTTP' },
    { port: 443, service: 'HTTPS' },
    { port: 445, service: 'SMB' },
    { port: 3389, service: 'RDP' },
    { port: 5900, service: 'VNC' },
    { port: 8080, service: 'HTTP-Proxy' },
    { port: 9100, service: 'Printer' },
    { port: 554, service: 'RTSP' },
    { port: 631, service: 'IPP' },
  ];
  
  const openPorts = [];
  
  // Quick port scan using TCP connect
  for (const { port, service } of commonPorts) {
    try {
      const isOpen = await checkPort(ip, port);
      if (isOpen) {
        openPorts.push(port);
      }
    } catch (error) {
      // Port closed or filtered
    }
  }
  
  return openPorts;
}

// Simple port checker
async function checkPort(host, port, timeout = 1000) {
  return new Promise((resolve) => {
    const net = require('net');
    const socket = new net.Socket();
    
    socket.setTimeout(timeout);
    
    socket.on('connect', () => {
      socket.destroy();
      resolve(true);
    });
    
    socket.on('timeout', () => {
      socket.destroy();
      resolve(false);
    });
    
    socket.on('error', () => {
      resolve(false);
    });
    
    socket.connect(port, host);
  });
}

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
    
    // Also discover the local machine
    await discoverLocalDevice(localIPs[0]);
    
    for (const localIP of localIPs) {
      const subnet = localIP.substring(0, localIP.lastIndexOf('.'));
      await scanSubnet(subnet);
    }
    
  } catch (error) {
    console.error('Error discovering network devices:', error);
  }
}

async function discoverLocalDevice(localIP) {
  try {
    const hostname = os.hostname();
    const networkInterfaces = os.networkInterfaces();
    let macAddress = 'Unknown';
    
    // Get MAC address of the primary interface
    for (const interfaces of Object.values(networkInterfaces)) {
      for (const iface of interfaces) {
        if (iface.family === 'IPv4' && !iface.internal && iface.mac) {
          macAddress = iface.mac.toUpperCase();
          break;
        }
      }
    }
    
    const device = {
      id: localIP,
      ipAddress: localIP,
      macAddress: macAddress,
      hostname: hostname,
      deviceType: 'Computer',
      position: { x: 0, y: 0, z: 0 },
      lastSeen: new Date(),
      firstSeen: new Date(),
      isOnline: true,
      responseTime: 0,
      vendor: deviceTypeDetector.getVendorFromMac(macAddress),
      openPorts: [80, 443], // Assume common ports
      isLocalDevice: true
    };
    
    devices.set(localIP, device);
    io.emit('deviceDiscovered', device);
    
  } catch (error) {
    console.error('Error discovering local device:', error);
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
      const resolved = await new Promise((resolve) => {
        dns.reverse(ip, (err, hostnames) => {
          resolve(hostnames && hostnames.length > 0 ? hostnames[0] : ip);
        });
      });
      hostname = resolved;
    } catch (e) {
      // Use IP if hostname resolution fails
    }
    
    // Scan for open ports (quick scan)
    const openPorts = await scanCommonPorts(ip);
    
    const deviceType = deviceTypeDetector.detectType(ip, hostname, macAddress, openPorts);
    const position = positionCalculator.calculatePosition(devices.size, deviceType);
    const vendor = deviceTypeDetector.getVendorFromMac(macAddress);
    
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
      vendor: vendor,
      openPorts: openPorts,
      isLocalDevice: false
    };
    
    const isNewDevice = !devices.has(ip);
    devices.set(ip, device);
    
    if (isNewDevice) {
      console.log(`Discovered new device: ${ip} (${hostname}) - ${deviceType} - ${vendor}`);
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
    
    // Simulate some peer-to-peer connections
    if (Math.random() > 0.7) {
      const peers = existingDevices.filter(d => 
        d.deviceType === 'Computer' && d.id !== device.id
      );
      if (peers.length > 0) {
        const peer = peers[Math.floor(Math.random() * peers.length)];
        createConnection(device, peer, 'Service');
      }
    }
  }
  
  // Printers connect to computers
  if (device.deviceType === 'Printer') {
    const computers = existingDevices.filter(d => d.deviceType === 'Computer');
    computers.slice(0, 3).forEach(computer => {
      createConnection(computer, device, 'Service');
    });
  }
}

function createConnection(sourceDevice, targetDevice, connectionType) {
  const connectionId = `${sourceDevice.id}-${targetDevice.id}`;
  
  if (!connections.has(connectionId)) {
    const protocols = ['TCP', 'UDP', 'HTTP', 'HTTPS', 'ICMP'];
    const protocol = connectionType === 'Gateway' ? 'TCP' : 
                    connectionType === 'Service' ? protocols[Math.floor(Math.random() * 3)] :
                    'HTTP';
    
    const connection = {
      id: connectionId,
      sourceIp: sourceDevice.ipAddress,
      destinationIp: targetDevice.ipAddress,
      sourceDevice: sourceDevice,
      targetDevice: targetDevice,
      connectionType: connectionType,
      protocol: protocol,
      state: 'Established',
      startTime: new Date(),
      lastActivity: new Date(),
      bytesTransferred: Math.floor(Math.random() * 10000000),
      packetsCount: Math.floor(Math.random() * 10000),
      isActive: true,
      port: protocol === 'HTTP' ? 80 : protocol === 'HTTPS' ? 443 : 0
    };
    
    connections.set(connectionId, connection);
    io.emit('connectionEstablished', connection);
  }
}

// Update network statistics
function updateNetworkStats() {
  const activeDevices = Array.from(devices.values()).filter(d => d.isOnline);
  const activeConnections = Array.from(connections.values()).filter(c => c.isActive);
  
  // Calculate protocol distribution
  const protocolCounts = {};
  activeConnections.forEach(conn => {
    protocolCounts[conn.protocol] = (protocolCounts[conn.protocol] || 0) + 1;
  });
  
  const total = activeConnections.length || 1;
  const protocolDistribution = {};
  Object.entries(protocolCounts).forEach(([protocol, count]) => {
    protocolDistribution[protocol] = (count / total) * 100;
  });
  
  networkStats = {
    totalDevices: activeDevices.length,
    activeConnections: activeConnections.length,
    packetsPerSecond: Math.floor(Math.random() * 100) + activeConnections.length * 10,
    bytesPerSecond: Math.floor(Math.random() * 1000000) + activeConnections.length * 100000,
    protocolDistribution: protocolDistribution
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
    if (!device.isLocalDevice) {
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