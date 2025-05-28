import React, { useRef, useMemo } from 'react';
import { useFrame } from '@react-three/fiber';
import { Text, Line, Sphere, Box } from '@react-three/drei';
import * as THREE from 'three';

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

interface NetworkSceneProps {
  devices: Device[];
  connections: Connection[];
  onDeviceClick: (device: Device) => void;
}

const DeviceNode: React.FC<{
  device: Device;
  onClick: (device: Device) => void;
}> = ({ device, onClick }) => {
  const meshRef = useRef<THREE.Mesh>(null);
  
  useFrame((state) => {
    if (meshRef.current) {
      // Gentle floating animation
      meshRef.current.position.y = device.position.y + Math.sin(state.clock.elapsedTime + device.position.x) * 0.5;
      
      // Pulse effect for online devices
      if (device.isOnline) {
        const scale = 1 + Math.sin(state.clock.elapsedTime * 2) * 0.1;
        meshRef.current.scale.setScalar(scale);
      }
    }
  });

  const getDeviceColor = (deviceType: string, isOnline: boolean) => {
    if (!isOnline) return '#666666';
    
    switch (deviceType) {
      case 'Router': return '#ff6b35';
      case 'Server': return '#f7931e';
      case 'Computer': return '#00bcd4';
      case 'MobilePhone': return '#e91e63';
      case 'Printer': return '#795548';
      case 'Camera': return '#9c27b0';
      case 'SmartTV': return '#3f51b5';
      case 'Switch': return '#4caf50';
      case 'AccessPoint': return '#009688';
      default: return '#607d8b';
    }
  };

  const getDeviceGeometry = (deviceType: string) => {
    switch (deviceType) {
      case 'Router':
      case 'Switch':
        return <Box args={[3, 1, 3]} />;
      case 'Server':
        return <Box args={[2, 4, 2]} />;
      case 'MobilePhone':
        return <Box args={[1, 3, 0.5]} />;
      case 'Printer':
        return <Box args={[3, 2, 2]} />;
      default:
        return <Sphere args={[1.5]} />;
    }
  };

  return (
    <group position={[device.position.x, device.position.y, device.position.z]}>
      <mesh
        ref={meshRef}
        onClick={() => onClick(device)}
        onPointerOver={(e) => {
          e.stopPropagation();
          document.body.style.cursor = 'pointer';
        }}
        onPointerOut={() => {
          document.body.style.cursor = 'auto';
        }}
      >
        {getDeviceGeometry(device.deviceType)}
        <meshStandardMaterial
          color={getDeviceColor(device.deviceType, device.isOnline)}
          emissive={device.isOnline ? getDeviceColor(device.deviceType, true) : '#000000'}
          emissiveIntensity={device.isOnline ? 0.2 : 0}
          roughness={0.3}
          metalness={0.7}
        />
      </mesh>
      
      {/* Device label */}
      <Text
        position={[0, 4, 0]}
        fontSize={1.5}
        color="white"
        anchorX="center"
        anchorY="middle"
        maxWidth={20}
        textAlign="center"
      >
        {device.hostname}
      </Text>
      
      {/* IP Address */}
      <Text
        position={[0, 2.5, 0]}
        fontSize={1}
        color="#cccccc"
        anchorX="center"
        anchorY="middle"
      >
        {device.ipAddress}
      </Text>
      
      {/* Device type indicator */}
      <Text
        position={[0, -3, 0]}
        fontSize={0.8}
        color={getDeviceColor(device.deviceType, device.isOnline)}
        anchorX="center"
        anchorY="middle"
      >
        {device.deviceType}
      </Text>
      
      {/* Online status indicator */}
      <Sphere args={[0.3]} position={[2.5, 2.5, 0]}>
        <meshBasicMaterial color={device.isOnline ? '#4caf50' : '#f44336'} />
      </Sphere>
    </group>
  );
};

const ConnectionLine: React.FC<{
  connection: Connection;
  devices: Device[];
}> = ({ connection, devices }) => {
  const lineRef = useRef<THREE.Group>(null);
  
  const sourceDevice = devices.find(d => d.ipAddress === connection.sourceIp);
  const targetDevice = devices.find(d => d.ipAddress === connection.destinationIp);
  
  useFrame((state) => {
    if (lineRef.current && connection.isActive) {
      // Animate connection activity
      const opacity = 0.3 + Math.sin(state.clock.elapsedTime * 3) * 0.2;
      lineRef.current.children.forEach((child: any) => {
        if (child.material) {
          child.material.opacity = opacity;
        }
      });
    }
  });

  if (!sourceDevice || !targetDevice) return null;

  const points = [
    new THREE.Vector3(sourceDevice.position.x, sourceDevice.position.y, sourceDevice.position.z),
    new THREE.Vector3(targetDevice.position.x, targetDevice.position.y, targetDevice.position.z)
  ];

  const getConnectionColor = (connectionType: string) => {
    switch (connectionType) {
      case 'Gateway': return '#ff9800';
      case 'Service': return '#2196f3';
      case 'Internet': return '#4caf50';
      default: return '#9e9e9e';
    }
  };

  return (
    <group ref={lineRef}>
      <Line
        points={points}
        color={getConnectionColor(connection.connectionType)}
        lineWidth={connection.isActive ? 2 : 1}
        transparent
        opacity={connection.isActive ? 0.6 : 0.3}
      />
      
      {/* Data flow animation particles */}
      {connection.isActive && (
        <DataFlowParticles
          start={points[0]}
          end={points[1]}
          color={getConnectionColor(connection.connectionType)}
        />
      )}
    </group>
  );
};

const DataFlowParticles: React.FC<{
  start: THREE.Vector3;
  end: THREE.Vector3;
  color: string;
}> = ({ start, end, color }) => {
  const particlesRef = useRef<THREE.Group>(null);
  
  useFrame((state) => {
    if (particlesRef.current) {
      particlesRef.current.children.forEach((particle, index) => {
        const progress = (state.clock.elapsedTime * 0.5 + index * 0.2) % 1;
        const position = new THREE.Vector3().lerpVectors(start, end, progress);
        particle.position.copy(position);
        
        // Fade particles at the ends
        const opacity = Math.sin(progress * Math.PI);
        (particle as any).material.opacity = opacity * 0.8;
      });
    }
  });

  const particles = useMemo(() => {
    const count = 3;
    return Array.from({ length: count }, (_, i) => (
      <Sphere key={i} args={[0.2]}>
        <meshBasicMaterial color={color} transparent opacity={0.8} />
      </Sphere>
    ));
  }, [color]);

  return <group ref={particlesRef}>{particles}</group>;
};

const NetworkGrid: React.FC = () => {
  const gridRef = useRef<THREE.Group>(null);
  
  useFrame((state) => {
    if (gridRef.current) {
      gridRef.current.rotation.y = state.clock.elapsedTime * 0.1;
    }
  });

  const gridLines = useMemo(() => {
    const lines = [];
    const size = 200;
    const divisions = 20;
    const step = size / divisions;
    
    // Create grid lines
    for (let i = -divisions; i <= divisions; i++) {
      const pos = i * step;
      
      // Horizontal lines
      lines.push(
        <Line
          key={`h-${i}`}
          points={[
            new THREE.Vector3(-size, 0, pos),
            new THREE.Vector3(size, 0, pos)
          ]}
          color="#333333"
          lineWidth={0.5}
          transparent
          opacity={0.3}
        />
      );
      
      // Vertical lines
      lines.push(
        <Line
          key={`v-${i}`}
          points={[
            new THREE.Vector3(pos, 0, -size),
            new THREE.Vector3(pos, 0, size)
          ]}
          color="#333333"
          lineWidth={0.5}
          transparent
          opacity={0.3}
        />
      );
    }
    
    return lines;
  }, []);

  return <group ref={gridRef}>{gridLines}</group>;
};

const NetworkScene: React.FC<NetworkSceneProps> = ({
  devices,
  connections,
  onDeviceClick
}) => {
  return (
    <>
      {/* Background grid */}
      <NetworkGrid />
      
      {/* Render devices */}
      {devices.map((device) => (
        <DeviceNode
          key={device.id}
          device={device}
          onClick={onDeviceClick}
        />
      ))}
      
      {/* Render connections */}
      {connections.map((connection) => (
        <ConnectionLine
          key={connection.id}
          connection={connection}
          devices={devices}
        />
      ))}
      
      {/* Central network indicator */}
      <group position={[0, 0, 0]}>
        <Sphere args={[2]}>
          <meshStandardMaterial
            color="#00bcd4"
            emissive="#00bcd4"
            emissiveIntensity={0.3}
            transparent
            opacity={0.6}
          />
        </Sphere>
        <Text
          position={[0, 0, 0]}
          fontSize={1}
          color="white"
          anchorX="center"
          anchorY="middle"
        >
          NETWORK
        </Text>
      </group>
    </>
  );
};

export default NetworkScene; 