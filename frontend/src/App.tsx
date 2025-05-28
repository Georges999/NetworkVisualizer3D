import React, { useState, useEffect } from 'react';
import {
  Box,
  AppBar,
  Toolbar,
  Typography,
  Paper,
  Button,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider,
  Container,
  ThemeProvider,
  createTheme,
  CssBaseline,
  Grid,
  IconButton,
  Drawer,
  Tooltip,
  Badge,
  Fab,
  Zoom,
  alpha,
} from '@mui/material';
import {
  NetworkCheck,
  Refresh,
  Search,
  DeviceHub,
  Computer,
  Smartphone,
  Router,
  Print,
  Tv,
  Videocam,
  Storage,
  WifiTethering,
  Menu as MenuIcon,
  Close,
  Info,
  Speed,
  Security,
  Cloud,
  Lan,
} from '@mui/icons-material';
import io, { Socket } from 'socket.io-client';
import NetworkVisualization from './components/NetworkVisualization';
import StatsPanel from './components/StatsPanel';
import DevicePanel from './components/DevicePanel';
import './App.css';

// Create a modern dark theme with matte colors
const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: {
      main: '#00ACC1', // Cyan
      light: '#5DDEF4',
      dark: '#007C91',
    },
    secondary: {
      main: '#FF6F00', // Deep Orange
      light: '#FF9E40',
      dark: '#C43E00',
    },
    background: {
      default: '#0A0E27',
      paper: '#0F1729',
    },
    text: {
      primary: '#E8EAED',
      secondary: '#9AA0A6',
    },
    error: {
      main: '#F44336',
    },
    warning: {
      main: '#FF9800',
    },
    info: {
      main: '#03A9F4',
    },
    success: {
      main: '#4CAF50',
    },
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    h1: {
      fontWeight: 600,
    },
    h2: {
      fontWeight: 600,
    },
    h3: {
      fontWeight: 600,
    },
    h4: {
      fontWeight: 600,
    },
    h5: {
      fontWeight: 600,
    },
    h6: {
      fontWeight: 600,
    },
  },
  shape: {
    borderRadius: 12,
  },
  components: {
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          backgroundColor: '#0F1729',
          border: '1px solid rgba(255, 255, 255, 0.05)',
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          backgroundColor: 'rgba(15, 23, 41, 0.95)',
          backdropFilter: 'blur(10px)',
          borderBottom: '1px solid rgba(255, 255, 255, 0.05)',
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          fontWeight: 500,
        },
      },
    },
  },
});

interface Device {
  id: string;
  ipAddress: string;
  macAddress: string;
  hostname: string;
  deviceType: string;
  position: { x: number; y: number; z: number };
  lastSeen: Date;
  firstSeen: Date;
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
  startTime: Date;
  lastActivity: Date;
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

const App: React.FC = () => {
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
  const [drawerOpen, setDrawerOpen] = useState(true);
  const [showInfo, setShowInfo] = useState(false);

  // Device type icons mapping
  const getDeviceIcon = (deviceType: string) => {
    switch (deviceType) {
      case 'Computer':
        return <Computer />;
      case 'MobilePhone':
        return <Smartphone />;
      case 'Tablet':
        return <Smartphone />;
      case 'Router':
        return <Router />;
      case 'Printer':
        return <Print />;
      case 'SmartTV':
        return <Tv />;
      case 'Camera':
        return <Videocam />;
      case 'Server':
        return <Storage />;
      case 'AccessPoint':
        return <WifiTethering />;
      default:
        return <DeviceHub />;
    }
  };

  // Device type colors
  const getDeviceColor = (deviceType: string) => {
    switch (deviceType) {
      case 'Computer':
        return theme.palette.info.main;
      case 'MobilePhone':
      case 'Tablet':
        return theme.palette.success.main;
      case 'Router':
        return theme.palette.primary.main;
      case 'Printer':
        return theme.palette.secondary.main;
      case 'SmartTV':
        return theme.palette.warning.main;
      case 'Camera':
        return theme.palette.error.main;
      case 'Server':
        return '#9C27B0';
      case 'AccessPoint':
        return '#00BCD4';
      default:
        return theme.palette.grey[500];
    }
  };

  useEffect(() => {
    const newSocket = io('http://localhost:5000');
    setSocket(newSocket);

    newSocket.on('connect', () => {
      console.log('Connected to backend');
    });

    newSocket.on('initialData', (data: { 
      devices: Device[], 
      connections: Connection[], 
      stats: NetworkStats 
    }) => {
      setDevices(data.devices);
      setConnections(data.connections);
      setStats(data.stats);
    });

    newSocket.on('deviceDiscovered', (device: Device) => {
      setDevices(prev => [...prev, device]);
    });

    newSocket.on('deviceUpdated', (updatedDevice: Device) => {
      setDevices(prev => prev.map(d => 
        d.id === updatedDevice.id ? updatedDevice : d
      ));
    });

    newSocket.on('connectionEstablished', (connection: Connection) => {
      setConnections(prev => [...prev, connection]);
    });

    newSocket.on('networkStats', (newStats: NetworkStats) => {
      setStats(newStats);
    });

    return () => {
      newSocket.close();
    };
  }, []);

  const handleScan = () => {
    if (socket) {
      setIsScanning(true);
      socket.emit('startScan');
      setTimeout(() => setIsScanning(false), 5000);
    }
  };

  // Group devices by type
  const devicesByType = devices.reduce((acc, device) => {
    if (!acc[device.deviceType]) {
      acc[device.deviceType] = [];
    }
    acc[device.deviceType].push(device);
    return acc;
  }, {} as { [key: string]: Device[] });

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box sx={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>
        <AppBar position="fixed" elevation={0}>
          <Toolbar>
            <IconButton
              edge="start"
              color="inherit"
              aria-label="menu"
              onClick={() => setDrawerOpen(!drawerOpen)}
              sx={{ mr: 2 }}
            >
              {drawerOpen ? <Close /> : <MenuIcon />}
            </IconButton>
            
            <NetworkCheck sx={{ mr: 2, color: theme.palette.primary.main }} />
            <Typography variant="h6" sx={{ flexGrow: 1, fontWeight: 600 }}>
              Network Visualizer 3D
            </Typography>
            
            <Tooltip title="Learn about your network">
              <IconButton
                color="inherit"
                onClick={() => setShowInfo(!showInfo)}
                sx={{ mr: 2 }}
              >
                <Info />
              </IconButton>
            </Tooltip>
            
            <Tooltip title="Scan network for devices">
              <Button
                variant="contained"
                startIcon={<Search />}
                onClick={handleScan}
                disabled={isScanning}
                sx={{
                  background: `linear-gradient(45deg, ${theme.palette.primary.main} 30%, ${theme.palette.primary.light} 90%)`,
                  boxShadow: '0 3px 5px 2px rgba(0, 172, 193, .3)',
                }}
              >
                {isScanning ? 'Scanning...' : 'Scan Network'}
              </Button>
            </Tooltip>
          </Toolbar>
        </AppBar>
        
        <Drawer
          anchor="left"
          open={drawerOpen}
          variant="persistent"
          sx={{
            width: drawerOpen ? 380 : 0,
            flexShrink: 0,
            '& .MuiDrawer-paper': {
              width: 380,
              boxSizing: 'border-box',
              backgroundColor: theme.palette.background.paper,
              borderRight: '1px solid rgba(255, 255, 255, 0.05)',
              transition: theme.transitions.create(['width', 'margin'], {
                easing: theme.transitions.easing.sharp,
                duration: theme.transitions.duration.enteringScreen,
              }),
            },
          }}
        >
          <Toolbar />
          <Box sx={{ overflow: 'auto', p: 2 }}>
            <StatsPanel stats={stats} />
            
            {selectedDevice && (
              <Zoom in={true}>
                <Box>
                  <DevicePanel
                    device={selectedDevice}
                    connections={connections.filter(
                      conn => conn.sourceIp === selectedDevice.ipAddress || 
                              conn.destinationIp === selectedDevice.ipAddress
                    )}
                    onClose={() => setSelectedDevice(null)}
                  />
                </Box>
              </Zoom>
            )}
            
            <Paper sx={{ p: 2, mt: 2 }}>
              <Typography variant="h6" sx={{ mb: 2, display: 'flex', alignItems: 'center' }}>
                <Lan sx={{ mr: 1, color: theme.palette.primary.main }} />
                Devices by Type
              </Typography>
              <List dense>
                {Object.entries(devicesByType).map(([type, typeDevices]) => (
                  <Box key={type}>
                    <ListItem sx={{ pl: 0 }}>
                      <ListItemIcon sx={{ minWidth: 36 }}>
                        <Badge badgeContent={typeDevices.length} color="primary">
                          <Box sx={{ color: getDeviceColor(type) }}>
                            {getDeviceIcon(type)}
                          </Box>
                        </Badge>
                      </ListItemIcon>
                      <ListItemText 
                        primary={type}
                        secondary={`${typeDevices.filter(d => d.isOnline).length} online`}
                      />
                    </ListItem>
                  </Box>
                ))}
              </List>
            </Paper>
          </Box>
        </Drawer>
        
        <Box
          component="main"
          sx={{
            flexGrow: 1,
            height: '100vh',
            overflow: 'hidden',
            backgroundColor: theme.palette.background.default,
            marginLeft: drawerOpen ? 0 : '-380px',
            transition: theme.transitions.create(['margin'], {
              easing: theme.transitions.easing.sharp,
              duration: theme.transitions.duration.leavingScreen,
            }),
          }}
        >
          <Toolbar />
          <NetworkVisualization
            devices={devices}
            connections={connections}
            onDeviceSelect={setSelectedDevice}
            selectedDevice={selectedDevice}
          />
          
          {showInfo && (
            <Paper
              sx={{
                position: 'absolute',
                top: 80,
                right: 16,
                width: 350,
                p: 3,
                backgroundColor: alpha(theme.palette.background.paper, 0.95),
                backdropFilter: 'blur(10px)',
              }}
            >
              <Typography variant="h6" sx={{ mb: 2 }}>
                Understanding Your Network
              </Typography>
              <Typography variant="body2" sx={{ mb: 2 }}>
                This visualization shows all devices connected to your network:
              </Typography>
              <Box sx={{ mb: 1 }}>
                <Typography variant="subtitle2" color="primary">
                  • Devices
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Each sphere represents a device (computer, phone, printer, etc.)
                </Typography>
              </Box>
              <Box sx={{ mb: 1 }}>
                <Typography variant="subtitle2" color="primary">
                  • Connections
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Lines show active network connections between devices
                </Typography>
              </Box>
              <Box sx={{ mb: 1 }}>
                <Typography variant="subtitle2" color="primary">
                  • Colors
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Different colors indicate different device types and connection states
                </Typography>
              </Box>
              <Box>
                <Typography variant="subtitle2" color="primary">
                  • Interaction
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Click on any device to see detailed information about it
                </Typography>
              </Box>
            </Paper>
          )}
        </Box>
      </Box>
    </ThemeProvider>
  );
};

export default App;
