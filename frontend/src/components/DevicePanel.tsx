import React, { useState } from 'react';
import {
  Paper,
  Typography,
  Box,
  IconButton,
  Chip,
  Divider,
  LinearProgress,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Grid,
  Tooltip,
  Avatar,
  Button,
  Tab,
  Tabs,
} from '@mui/material';
import {
  Close,
  Computer,
  Smartphone,
  Router,
  Print,
  Tv,
  Videocam,
  Storage,
  WifiTethering,
  DeviceHub,
  ExpandMore,
  AccessTime,
  Speed,
  Memory,
  Security,
  Lan,
  Cloud,
  Description,
  Warning,
  CheckCircle,
  Error,
  WifiPassword,
  Http,
  Dns,
  VpnKey,
  DataUsage,
  NetworkCell,
  SignalCellularAlt,
  BarChart,
} from '@mui/icons-material';

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

interface DevicePanelProps {
  device: Device;
  connections: Connection[];
  onClose: () => void;
}

const DevicePanel: React.FC<DevicePanelProps> = ({ device, connections, onClose }) => {
  const [tabValue, setTabValue] = useState(0);

  const getDeviceIcon = (deviceType: string) => {
    const iconProps = { sx: { fontSize: 40 } };
    switch (deviceType) {
      case 'Computer':
        return <Computer {...iconProps} />;
      case 'MobilePhone':
        return <Smartphone {...iconProps} />;
      case 'Tablet':
        return <Smartphone {...iconProps} />;
      case 'Router':
        return <Router {...iconProps} />;
      case 'Printer':
        return <Print {...iconProps} />;
      case 'SmartTV':
        return <Tv {...iconProps} />;
      case 'Camera':
        return <Videocam {...iconProps} />;
      case 'Server':
        return <Storage {...iconProps} />;
      case 'AccessPoint':
        return <WifiTethering {...iconProps} />;
      default:
        return <DeviceHub {...iconProps} />;
    }
  };

  const formatUptime = (firstSeen: Date, lastSeen: Date) => {
    const now = new Date();
    const uptimeMs = now.getTime() - new Date(firstSeen).getTime();
    const days = Math.floor(uptimeMs / (1000 * 60 * 60 * 24));
    const hours = Math.floor((uptimeMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    const minutes = Math.floor((uptimeMs % (1000 * 60 * 60)) / (1000 * 60));
    
    if (days > 0) return `${days}d ${hours}h`;
    if (hours > 0) return `${hours}h ${minutes}m`;
    return `${minutes}m`;
  };

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const getDeviceDescription = (deviceType: string) => {
    const descriptions: { [key: string]: string } = {
      'Computer': 'A desktop or laptop computer on your network',
      'MobilePhone': 'A smartphone connected via Wi-Fi',
      'Tablet': 'A tablet device connected via Wi-Fi',
      'Router': 'The main gateway that connects your network to the internet',
      'Printer': 'A network-enabled printer for printing documents',
      'SmartTV': 'A smart TV that can stream content from the internet',
      'Camera': 'A security camera or webcam on the network',
      'Server': 'A computer that provides services to other devices',
      'AccessPoint': 'A device that extends your Wi-Fi coverage',
    };
    return descriptions[deviceType] || 'A device connected to your network';
  };

  const getConnectionTypeDescription = (type: string) => {
    const descriptions: { [key: string]: string } = {
      'Gateway': 'Main internet connection through router',
      'Service': 'Providing or using network services',
      'Internet': 'Accessing the internet',
    };
    return descriptions[type] || type;
  };

  const getProtocolDescription = (protocol: string) => {
    const descriptions: { [key: string]: string } = {
      'TCP': 'Reliable data transmission (web browsing, email)',
      'UDP': 'Fast data transmission (video streaming, gaming)',
      'HTTP': 'Web traffic (unsecured)',
      'HTTPS': 'Secure web traffic (encrypted)',
      'ICMP': 'Network diagnostics (ping)',
    };
    return descriptions[protocol] || protocol;
  };

  const activeConnections = connections.filter(c => c.isActive);
  const totalBytesTransferred = connections.reduce((sum, c) => sum + c.bytesTransferred, 0);

  return (
    <Paper 
      sx={{ 
        p: 3, 
        mb: 2,
        background: 'linear-gradient(135deg, rgba(15, 23, 41, 0.9) 0%, rgba(15, 23, 41, 0.95) 100%)',
        border: '1px solid rgba(0, 172, 193, 0.3)',
      }}
    >
      {/* Header */}
      <Box display="flex" alignItems="center" justifyContent="space-between" mb={2}>
        <Box display="flex" alignItems="center">
          <Avatar
            sx={{
              bgcolor: device.isOnline ? 'success.main' : 'error.main',
              width: 56,
              height: 56,
              mr: 2,
            }}
          >
            {getDeviceIcon(device.deviceType)}
          </Avatar>
          <Box>
            <Typography variant="h6" fontWeight={600}>
              {device.hostname}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {getDeviceDescription(device.deviceType)}
            </Typography>
          </Box>
        </Box>
        <IconButton onClick={onClose} size="small">
          <Close />
        </IconButton>
      </Box>

      {/* Status Bar */}
      <Box display="flex" gap={1} mb={2}>
        <Chip
          icon={device.isOnline ? <CheckCircle /> : <Error />}
          label={device.isOnline ? 'Online' : 'Offline'}
          color={device.isOnline ? 'success' : 'error'}
          size="small"
        />
        <Chip
          icon={<Speed />}
          label={`${device.responseTime}ms`}
          size="small"
          color="primary"
        />
        <Chip
          icon={<AccessTime />}
          label={`Up ${formatUptime(device.firstSeen, device.lastSeen)}`}
          size="small"
        />
      </Box>

      <Divider sx={{ my: 2 }} />

      {/* Tabs */}
      <Tabs value={tabValue} onChange={(e, v) => setTabValue(v)} sx={{ mb: 2 }}>
        <Tab label="Overview" />
        <Tab label="Connections" />
        <Tab label="Activity" />
      </Tabs>

      {/* Tab Content */}
      {tabValue === 0 && (
        <Box>
          <Grid container spacing={2}>
            <Grid item xs={12}>
              <Accordion defaultExpanded>
                <AccordionSummary expandIcon={<ExpandMore />}>
                  <Typography variant="subtitle1" fontWeight={500}>
                    <Lan sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Network Information
                  </Typography>
                </AccordionSummary>
                <AccordionDetails>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <Tooltip title="The unique network address of this device">
                          <Dns />
                        </Tooltip>
                      </ListItemIcon>
                      <ListItemText
                        primary="IP Address"
                        secondary={device.ipAddress}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Tooltip title="The hardware address that uniquely identifies this device">
                          <VpnKey />
                        </Tooltip>
                      </ListItemIcon>
                      <ListItemText
                        primary="MAC Address"
                        secondary={device.macAddress}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Tooltip title="The name this device uses on the network">
                          <Description />
                        </Tooltip>
                      </ListItemIcon>
                      <ListItemText
                        primary="Hostname"
                        secondary={device.hostname}
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Tooltip title="The manufacturer of this device">
                          <Memory />
                        </Tooltip>
                      </ListItemIcon>
                      <ListItemText
                        primary="Vendor"
                        secondary={device.vendor || 'Unknown'}
                      />
                    </ListItem>
                  </List>
                </AccordionDetails>
              </Accordion>
            </Grid>

            <Grid item xs={12}>
              <Accordion>
                <AccordionSummary expandIcon={<ExpandMore />}>
                  <Typography variant="subtitle1" fontWeight={500}>
                    <Security sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Security Status
                  </Typography>
                </AccordionSummary>
                <AccordionDetails>
                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <WifiPassword />
                      </ListItemIcon>
                      <ListItemText
                        primary="Connection Security"
                        secondary="WPA2 Encrypted"
                      />
                    </ListItem>
                    <ListItem>
                      <ListItemIcon>
                        <Http />
                      </ListItemIcon>
                      <ListItemText
                        primary="Open Ports"
                        secondary="80 (HTTP), 443 (HTTPS), 22 (SSH)"
                      />
                    </ListItem>
                  </List>
                </AccordionDetails>
              </Accordion>
            </Grid>
          </Grid>
        </Box>
      )}

      {tabValue === 1 && (
        <Box>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            Active Connections: {activeConnections.length}
          </Typography>
          <List dense>
            {connections.map((connection) => (
              <ListItem key={connection.id} sx={{ pl: 0 }}>
                <ListItemIcon>
                  <NetworkCell color={connection.isActive ? 'primary' : 'disabled'} />
                </ListItemIcon>
                <ListItemText
                  primary={
                    <Box display="flex" alignItems="center" gap={1}>
                      <Typography variant="body2">
                        {connection.sourceIp === device.ipAddress 
                          ? `→ ${connection.destinationIp}` 
                          : `← ${connection.sourceIp}`}
                      </Typography>
                      <Chip 
                        label={connection.protocol} 
                        size="small" 
                        color="primary"
                        variant="outlined"
                      />
                    </Box>
                  }
                  secondary={
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        {getProtocolDescription(connection.protocol)}
                      </Typography>
                      <Typography variant="caption" display="block">
                        {formatBytes(connection.bytesTransferred)} transferred
                      </Typography>
                    </Box>
                  }
                />
              </ListItem>
            ))}
          </List>
        </Box>
      )}

      {tabValue === 2 && (
        <Box>
          <Grid container spacing={2}>
            <Grid item xs={6}>
              <Box textAlign="center" p={2}>
                <DataUsage sx={{ fontSize: 40, color: 'primary.main', mb: 1 }} />
                <Typography variant="h6">{formatBytes(totalBytesTransferred)}</Typography>
                <Typography variant="caption" color="text.secondary">
                  Total Data Transferred
                </Typography>
              </Box>
            </Grid>
            <Grid item xs={6}>
              <Box textAlign="center" p={2}>
                <SignalCellularAlt sx={{ fontSize: 40, color: 'success.main', mb: 1 }} />
                <Typography variant="h6">{connections.length}</Typography>
                <Typography variant="caption" color="text.secondary">
                  Total Connections
                </Typography>
              </Box>
            </Grid>
          </Grid>
          
          <Box mt={2}>
            <Typography variant="subtitle2" gutterBottom>
              Network Activity Timeline
            </Typography>
            <LinearProgress variant="determinate" value={75} sx={{ mb: 1 }} />
            <Typography variant="caption" color="text.secondary">
              Most active during: 2PM - 5PM
            </Typography>
          </Box>
        </Box>
      )}
    </Paper>
  );
};

export default DevicePanel; 