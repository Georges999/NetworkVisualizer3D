import React from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  IconButton,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider
} from '@mui/material';
import {
  Close,
  Computer,
  Router,
  Smartphone,
  Print,
  Tv,
  Camera,
  Storage,
  NetworkCheck,
  AccessTime,
  Speed
} from '@mui/icons-material';

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

interface DevicePanelProps {
  device: Device;
  connections: Connection[];
  onClose: () => void;
}

const DevicePanel: React.FC<DevicePanelProps> = ({ device, connections, onClose }) => {
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

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  const getConnectionTypeColor = (connectionType: string) => {
    switch (connectionType) {
      case 'Gateway': return '#ff9800';
      case 'Service': return '#2196f3';
      case 'Internet': return '#4caf50';
      default: return '#9e9e9e';
    }
  };

  return (
    <Card>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Box display="flex" alignItems="center">
            {getDeviceIcon(device.deviceType)}
            <Typography variant="h6" sx={{ ml: 1 }}>
              Device Details
            </Typography>
          </Box>
          <IconButton onClick={onClose} size="small">
            <Close />
          </IconButton>
        </Box>

        <Box mb={2}>
          <Typography variant="h5" gutterBottom>
            {device.hostname}
          </Typography>
          <Chip
            label={device.isOnline ? 'Online' : 'Offline'}
            color={device.isOnline ? 'success' : 'error'}
            size="small"
            sx={{ mb: 1 }}
          />
          <Chip
            label={device.deviceType}
            variant="outlined"
            size="small"
            sx={{ ml: 1, mb: 1 }}
          />
        </Box>

        <Box mb={3}>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            Network Information
          </Typography>
          <Typography variant="body2" gutterBottom>
            <strong>IP Address:</strong> {device.ipAddress}
          </Typography>
          <Typography variant="body2" gutterBottom>
            <strong>MAC Address:</strong> {device.macAddress}
          </Typography>
          <Typography variant="body2" gutterBottom>
            <strong>Vendor:</strong> {device.vendor}
          </Typography>
          <Typography variant="body2" gutterBottom>
            <strong>Response Time:</strong> {device.responseTime}ms
          </Typography>
        </Box>

        <Box mb={3}>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            Activity
          </Typography>
          <Box display="flex" alignItems="center" mb={1}>
            <AccessTime sx={{ mr: 1, fontSize: 16 }} />
            <Typography variant="body2">
              <strong>First Seen:</strong> {formatDate(device.firstSeen)}
            </Typography>
          </Box>
          <Box display="flex" alignItems="center">
            <NetworkCheck sx={{ mr: 1, fontSize: 16 }} />
            <Typography variant="body2">
              <strong>Last Seen:</strong> {formatDate(device.lastSeen)}
            </Typography>
          </Box>
        </Box>

        <Box mb={2}>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            Connections ({connections.length})
          </Typography>
          {connections.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              No active connections
            </Typography>
          ) : (
            <List dense>
              {connections.map((connection) => {
                const isSource = connection.sourceIp === device.ipAddress;
                const otherDevice = isSource ? connection.targetDevice : connection.sourceDevice;
                const direction = isSource ? '→' : '←';
                
                return (
                  <React.Fragment key={connection.id}>
                    <ListItem>
                      <ListItemIcon>
                        {getDeviceIcon(otherDevice.deviceType)}
                      </ListItemIcon>
                      <ListItemText
                        primary={
                          <Box display="flex" alignItems="center">
                            <Typography variant="body2">
                              {direction} {otherDevice.hostname}
                            </Typography>
                            <Chip
                              label={connection.connectionType}
                              size="small"
                              sx={{
                                ml: 1,
                                backgroundColor: getConnectionTypeColor(connection.connectionType),
                                color: 'white',
                                fontSize: '0.7rem'
                              }}
                            />
                          </Box>
                        }
                        secondary={
                          <Box>
                            <Typography variant="caption" display="block">
                              {otherDevice.ipAddress} • {connection.protocol}
                            </Typography>
                            <Typography variant="caption" display="block">
                              {formatBytes(connection.bytesTransferred)} • {connection.packetsCount} packets
                            </Typography>
                          </Box>
                        }
                      />
                      <Box display="flex" alignItems="center">
                        <Chip
                          label={connection.isActive ? 'Active' : 'Inactive'}
                          color={connection.isActive ? 'success' : 'default'}
                          size="small"
                          variant="outlined"
                        />
                      </Box>
                    </ListItem>
                    <Divider />
                  </React.Fragment>
                );
              })}
            </List>
          )}
        </Box>
      </CardContent>
    </Card>
  );
};

export default DevicePanel; 