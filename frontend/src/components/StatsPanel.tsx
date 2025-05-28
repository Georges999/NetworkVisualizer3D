import React from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  LinearProgress,
  Chip,
  Grid
} from '@mui/material';
import {
  Computer,
  NetworkCheck,
  Speed,
  DataUsage
} from '@mui/icons-material';

interface NetworkStats {
  totalDevices: number;
  activeConnections: number;
  packetsPerSecond: number;
  bytesPerSecond: number;
  protocolDistribution: { [key: string]: number };
}

interface StatsPanelProps {
  stats: NetworkStats;
}

const StatsPanel: React.FC<StatsPanelProps> = ({ stats }) => {
  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const getProtocolColor = (protocol: string) => {
    switch (protocol) {
      case 'TCP': return '#2196f3';
      case 'UDP': return '#ff9800';
      case 'HTTP': return '#4caf50';
      case 'HTTPS': return '#8bc34a';
      case 'ICMP': return '#f44336';
      default: return '#9e9e9e';
    }
  };

  return (
    <Card sx={{ mb: 2 }}>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Network Statistics
        </Typography>
        
        <Grid container spacing={2}>
          <Grid item xs={6}>
            <Box display="flex" alignItems="center" mb={1}>
              <Computer sx={{ mr: 1, color: '#2196f3' }} />
              <Box>
                <Typography variant="h4" component="div">
                  {stats.totalDevices}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Devices
                </Typography>
              </Box>
            </Box>
          </Grid>
          
          <Grid item xs={6}>
            <Box display="flex" alignItems="center" mb={1}>
              <NetworkCheck sx={{ mr: 1, color: '#4caf50' }} />
              <Box>
                <Typography variant="h4" component="div">
                  {stats.activeConnections}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Connections
                </Typography>
              </Box>
            </Box>
          </Grid>
          
          <Grid item xs={6}>
            <Box display="flex" alignItems="center" mb={1}>
              <Speed sx={{ mr: 1, color: '#ff9800' }} />
              <Box>
                <Typography variant="h4" component="div">
                  {stats.packetsPerSecond.toFixed(0)}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Packets/sec
                </Typography>
              </Box>
            </Box>
          </Grid>
          
          <Grid item xs={6}>
            <Box display="flex" alignItems="center" mb={1}>
              <DataUsage sx={{ mr: 1, color: '#9c27b0' }} />
              <Box>
                <Typography variant="h4" component="div">
                  {formatBytes(stats.bytesPerSecond)}/s
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Throughput
                </Typography>
              </Box>
            </Box>
          </Grid>
        </Grid>

        <Box mt={3}>
          <Typography variant="subtitle1" gutterBottom>
            Protocol Distribution
          </Typography>
          {Object.entries(stats.protocolDistribution).map(([protocol, percentage]) => (
            <Box key={protocol} mb={1}>
              <Box display="flex" justifyContent="space-between" alignItems="center" mb={0.5}>
                <Typography variant="body2">{protocol}</Typography>
                <Typography variant="body2">{percentage.toFixed(1)}%</Typography>
              </Box>
              <LinearProgress
                variant="determinate"
                value={percentage}
                sx={{
                  height: 6,
                  borderRadius: 3,
                  backgroundColor: '#e0e0e0',
                  '& .MuiLinearProgress-bar': {
                    backgroundColor: getProtocolColor(protocol),
                    borderRadius: 3,
                  },
                }}
              />
            </Box>
          ))}
        </Box>

        <Box mt={2} display="flex" flexWrap="wrap" gap={1}>
          {Object.entries(stats.protocolDistribution).map(([protocol, percentage]) => (
            <Chip
              key={protocol}
              label={`${protocol}: ${percentage.toFixed(1)}%`}
              size="small"
              sx={{
                backgroundColor: getProtocolColor(protocol),
                color: 'white',
                fontWeight: 'bold'
              }}
            />
          ))}
        </Box>
      </CardContent>
    </Card>
  );
};

export default StatsPanel; 