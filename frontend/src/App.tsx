import React, { useState, useEffect } from 'react';
import { Canvas } from '@react-three/fiber';
import { OrbitControls, Text, Html } from '@react-three/drei';
import { io, Socket } from 'socket.io-client';
import {
  AppBar,
  Toolbar,
  Typography,
  Box,
  Paper,
  Grid,
  Card,
  CardContent,
  Button,
  Chip,
  LinearProgress,
  IconButton,
  Drawer,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider
} from '@mui/material';
import {
  Computer,
  Router,
  Smartphone,
  Print,
  Tv,
  Camera,
  Storage,
  NetworkCheck,
  PlayArrow,
  Stop,
  Settings,
  Menu,
  Close
} from '@mui/icons-material';
import NetworkScene from './components/NetworkScene';
import DevicePanel from './components/DevicePanel';
import StatsPanel from './components/StatsPanel';
import './App.css';

interface Device {
  id: string;
  ipAddress: string;
  macAddress: string;
  hostname: string;
  deviceType: string;
  position: { x: number; y: number; z: number };
  lastSeen: string;
  firstSeen: string;
  isOnline: boolean;
  responseTime: number;
  vendor: string;
}

interface Connection {
  id: string;
  sourceIp: string;
  destinationIp: string;
  sourceDevice: Device;
  targetDevice: Device;
  connectionType: string;
  protocol: string;
  state: string;
  startTime: string;
  lastActivity: string;
  bytesTransferred: number;
  packetsCount: number;
  isActive: boolean;
}

interface NetworkStats {
  totalDevices: number;
  activeConnections: number;
  packetsPerSecond: number;
  bytesPerSecond: number;
  protocolDistribution: { [key: string]: number };
}

function App() {
  const [socket, setSocket] = useState<Socket | null>(null);
  const [devices, setDevices] = useState<Device[]>([]);
  const [connections, setConnections] = useState<Connection[]>([]);
  const [stats, setStats] = useState<NetworkStats>({
    totalDevices: 0,
    activeConnections: 0,
    packetsPerSecond: 0,
    bytesPerSecond: 0,
    protocolDistribution: {}
  });
  const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);
  const [isScanning, setIsScanning] = useState(false);
  const [isConnected, setIsConnected] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);

  useEffect(() => {
    // Connect to backend
    const newSocket = io('http://localhost:5000');
    setSocket(newSocket);

    newSocket.on('connect', () => {
      console.log('Connected to backend');
      setIsConnected(true);
    });

    newSocket.on('disconnect', () => {
      console.log('Disconnected from backend');
      setIsConnected(false);
    });

    newSocket.on('initialData', (data: {
      devices: Device[];
      connections: Connection[];
      stats: NetworkStats;
    }) => {
      setDevices(data.devices);
      setConnections(data.connections);
      setStats(data.stats);
    });

    newSocket.on('deviceDiscovered', (device: Device) => {
      setDevices(prev => {
        const existing = prev.find(d => d.id === device.id);
        if (existing) {
          return prev.map(d => d.id === device.id ? device : d);
        }
        return [...prev, device];
      });
    });

    newSocket.on('deviceUpdated', (device: Device) => {
      setDevices(prev => prev.map(d => d.id === device.id ? device : d));
    });

    newSocket.on('connectionEstablished', (connection: Connection) => {
      setConnections(prev => {
        const existing = prev.find(c => c.id === connection.id);
        if (existing) {
          return prev.map(c => c.id === connection.id ? connection : c);
        }
        return [...prev, connection];
      });
    });

    newSocket.on('networkStats', (newStats: NetworkStats) => {
      setStats(newStats);
    });

    return () => {
      newSocket.close();
    };
  }, []);

  const handleStartScan = () => {
    if (socket) {
      setIsScanning(true);
      socket.emit('startScan');
      setTimeout(() => setIsScanning(false), 5000); // Reset after 5 seconds
    }
  };

  const getDeviceIcon = (deviceType: string) => {
    switch (deviceType) {
      case 'Router': return <Router />;
      case 'Computer': return <Computer />;
      case 'MobilePhone': return <Smartphone />;
      case 'Printer': return <Print />;
      case 'SmartTV': return <Tv />;
      case 'Camera': return <Camera />;
      case 'Server': return <Storage />;
      default: return <Computer />;
    }
  };

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <div className="App">
      <AppBar position="fixed" sx={{ zIndex: 1300 }}>
        <Toolbar>
          <IconButton
            edge="start"
            color="inherit"
            onClick={() => setDrawerOpen(true)}
            sx={{ mr: 2 }}
          >
            <Menu />
          </IconButton>
          <NetworkCheck sx={{ mr: 2 }} />
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            NetworkVisualizer3D
          </Typography>
          <Chip
            label={isConnected ? 'Connected' : 'Disconnected'}
            color={isConnected ? 'success' : 'error'}
            variant="outlined"
            sx={{ mr: 2 }}
          />
          <Button
            variant="contained"
            color={isScanning ? 'secondary' : 'primary'}
            startIcon={isScanning ? <Stop /> : <PlayArrow />}
            onClick={handleStartScan}
            disabled={!isConnected}
          >
            {isScanning ? 'Scanning...' : 'Start Scan'}
          </Button>
        </Toolbar>
      </AppBar>

      <Drawer
        anchor="left"
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        sx={{ zIndex: 1200 }}
      >
        <Box sx={{ width: 300, pt: 8 }}>
          <List>
            <ListItem>
              <ListItemText
                primary="Network Devices"
                secondary={`${devices.length} devices discovered`}
              />
            </ListItem>
            <Divider />
            {devices.map((device) => (
              <ListItem
                key={device.id}
                component="div"
                onClick={() => {
                  setSelectedDevice(device);
                  setDrawerOpen(false);
                }}
              >
                <ListItemIcon>
                  {getDeviceIcon(device.deviceType)}
                </ListItemIcon>
                <ListItemText
                  primary={device.hostname}
                  secondary={`${device.ipAddress} - ${device.deviceType}`}
                />
                <Chip
                  size="small"
                  label={device.isOnline ? 'Online' : 'Offline'}
                  color={device.isOnline ? 'success' : 'error'}
                />
              </ListItem>
            ))}
          </List>
        </Box>
      </Drawer>

      <Box sx={{ pt: 8, height: '100vh', display: 'flex' }}>
        <Box sx={{ flex: 1, position: 'relative' }}>
          <Canvas
            camera={{ position: [100, 100, 100], fov: 60 }}
            style={{ background: 'linear-gradient(to bottom, #0a0a0a, #1a1a2e)' }}
          >
            <ambientLight intensity={0.3} />
            <pointLight position={[100, 100, 100]} intensity={1} />
            <pointLight position={[-100, -100, -100]} intensity={0.5} />
            <OrbitControls
              enablePan={true}
              enableZoom={true}
              enableRotate={true}
              maxDistance={500}
              minDistance={50}
            />
            <NetworkScene
              devices={devices}
              connections={connections}
              onDeviceClick={setSelectedDevice}
            />
          </Canvas>

          {isScanning && (
            <Box
              sx={{
                position: 'absolute',
                top: 16,
                left: 16,
                right: 16,
                zIndex: 1000
              }}
            >
              <Paper sx={{ p: 2 }}>
                <Typography variant="body2" gutterBottom>
                  Scanning network for devices...
                </Typography>
                <LinearProgress />
              </Paper>
            </Box>
          )}
        </Box>

        <Box sx={{ width: 400, p: 2, overflow: 'auto' }}>
          <Grid container spacing={2}>
            <Grid item xs={12}>
              <StatsPanel stats={stats} />
            </Grid>
            {selectedDevice && (
              <Grid item xs={12}>
                <DevicePanel
                  device={selectedDevice}
                  connections={connections.filter(
                    c => c.sourceIp === selectedDevice.ipAddress ||
                         c.destinationIp === selectedDevice.ipAddress
                  )}
                  onClose={() => setSelectedDevice(null)}
                />
              </Grid>
            )}
          </Grid>
        </Box>
      </Box>
    </div>
  );
}

export default App;
