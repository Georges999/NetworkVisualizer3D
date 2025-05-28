import React from 'react';
import {
  Paper,
  Typography,
  Box,
  LinearProgress,
  Chip,
  Grid,
} from '@mui/material';
import {
  Computer,
  NetworkCheck,
  Speed,
  DataUsage,
  ShowChart,
} from '@mui/icons-material';

interface StatsProps {
  stats: {
    totalDevices: number;
    activeConnections: number;
    packetsPerSecond: number;
    bytesPerSecond: number;
    protocolDistribution: { [key: string]: number };
  };
}

const StatsPanel: React.FC<StatsProps> = ({ stats }) => {
  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const formatNumber = (num: number) => {
    return new Intl.NumberFormat().format(num);
  };

  const protocolColors: { [key: string]: string } = {
    TCP: '#2196f3',
    UDP: '#4caf50',
    ICMP: '#ff9800',
    HTTP: '#f44336',
    HTTPS: '#9c27b0',
  };

  return (
    <Paper sx={{ p: 3, mb: 2 }}>
      <Box display="flex" alignItems="center" mb={2}>
        <ShowChart sx={{ mr: 1, color: '#1976d2' }} />
        <Typography variant="h6">Network Statistics</Typography>
      </Box>
      
      <Box>
        <Grid container spacing={2}>
          <Grid item xs={6}>
            <Box display="flex" alignItems="center" mb={1}>
              <Computer sx={{ mr: 1, color: '#2196f3' }} />
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Total Devices
                </Typography>
                <Typography variant="h6">
                  {stats.totalDevices}
                </Typography>
              </Box>
            </Box>
          </Grid>
          
          <Grid item xs={6}>
            <Box display="flex" alignItems="center" mb={1}>
              <NetworkCheck sx={{ mr: 1, color: '#4caf50' }} />
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Active Connections
                </Typography>
                <Typography variant="h6">
                  {stats.activeConnections}
                </Typography>
              </Box>
            </Box>
          </Grid>
          
          <Grid item xs={6}>
            <Box display="flex" alignItems="center" mb={1}>
              <Speed sx={{ mr: 1, color: '#ff9800' }} />
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Packets/sec
                </Typography>
                <Typography variant="h6">
                  {formatNumber(stats.packetsPerSecond)}
                </Typography>
              </Box>
            </Box>
          </Grid>
          
          <Grid item xs={6}>
            <Box display="flex" alignItems="center" mb={1}>
              <DataUsage sx={{ mr: 1, color: '#9c27b0' }} />
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Bandwidth
                </Typography>
                <Typography variant="h6">
                  {formatBytes(stats.bytesPerSecond)}/s
                </Typography>
              </Box>
            </Box>
          </Grid>
        </Grid>
        
        <Box mt={3}>
          <Typography variant="subtitle2" gutterBottom>
            Protocol Distribution
          </Typography>
          {Object.entries(stats.protocolDistribution).map(([protocol, percentage]) => (
            <Box key={protocol} mb={1}>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={0.5}>
                <Chip
                  label={protocol}
                  size="small"
                  sx={{
                    backgroundColor: protocolColors[protocol] || '#757575',
                    color: 'white',
                  }}
                />
                <Typography variant="body2" color="text.secondary">
                  {percentage.toFixed(1)}%
                </Typography>
              </Box>
              <LinearProgress
                variant="determinate"
                value={percentage}
                sx={{
                  height: 6,
                  borderRadius: 3,
                  backgroundColor: '#e0e0e0',
                  '& .MuiLinearProgress-bar': {
                    backgroundColor: protocolColors[protocol] || '#757575',
                  },
                }}
              />
            </Box>
          ))}
        </Box>
      </Box>
    </Paper>
  );
};

export default StatsPanel; 