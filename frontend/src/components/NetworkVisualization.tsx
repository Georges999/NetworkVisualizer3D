import React, { useRef, useMemo, useEffect, useState } from 'react';
import { Canvas, useFrame, useThree } from '@react-three/fiber';
import { 
  OrbitControls, 
  Text, 
  Html, 
  Float,
  Environment,
  ContactShadows,
  Sparkles,
  Trail,
  MeshDistortMaterial,
  GradientTexture,
  useHelper,
} from '@react-three/drei';
import * as THREE from 'three';
import { Box } from '@mui/material';

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

interface NetworkVisualizationProps {
  devices: Device[];
  connections: Connection[];
  onDeviceSelect: (device: Device | null) => void;
  selectedDevice: Device | null;
}

// Device Node Component
const DeviceNode: React.FC<{
  device: Device;
  onClick: () => void;
  isSelected: boolean;
}> = ({ device, onClick, isSelected }) => {
  const meshRef = useRef<THREE.Mesh>(null);
  const [hovered, setHovered] = useState(false);
  
  const color = useMemo(() => {
    const colors: { [key: string]: string } = {
      Router: '#00ACC1',
      Computer: '#03A9F4',
      MobilePhone: '#4CAF50',
      Tablet: '#4CAF50',
      Printer: '#FF6F00',
      SmartTV: '#FF9800',
      Camera: '#F44336',
      Server: '#9C27B0',
      AccessPoint: '#00BCD4',
    };
    return colors[device.deviceType] || '#757575';
  }, [device.deviceType]);

  const size = useMemo(() => {
    const sizes: { [key: string]: number } = {
      Router: 3,
      Server: 2.5,
      Computer: 2,
      AccessPoint: 2.5,
      MobilePhone: 1.5,
      Tablet: 1.8,
      Printer: 1.8,
      SmartTV: 2.2,
      Camera: 1.5,
    };
    return sizes[device.deviceType] || 2;
  }, [device.deviceType]);

  useFrame((state) => {
    if (meshRef.current) {
      // Gentle floating animation
      meshRef.current.position.y = device.position.y + Math.sin(state.clock.elapsedTime + device.position.x) * 0.3;
      
      // Rotation for selected or special devices
      if (isSelected || device.deviceType === 'Router' || device.deviceType === 'Server') {
        meshRef.current.rotation.y += 0.01;
      }
      
      // Pulse effect for active devices
      if (device.isOnline) {
        const scale = 1 + Math.sin(state.clock.elapsedTime * 2) * 0.05;
        meshRef.current.scale.setScalar(scale);
      }
    }
  });

  return (
    <Float
      speed={1}
      rotationIntensity={0.2}
      floatIntensity={0.5}
    >
      <mesh
        ref={meshRef}
        position={[device.position.x, device.position.y, device.position.z]}
        onClick={(e) => {
          e.stopPropagation();
          onClick();
        }}
        onPointerOver={() => setHovered(true)}
        onPointerOut={() => setHovered(false)}
      >
        {device.deviceType === 'Router' || device.deviceType === 'Server' ? (
          <boxGeometry args={[size, size, size]} />
        ) : (
          <sphereGeometry args={[size, 32, 32]} />
        )}
        
        <meshPhysicalMaterial
          color={color}
          emissive={color}
          emissiveIntensity={device.isOnline ? 0.2 : 0}
          metalness={0.7}
          roughness={0.3}
          clearcoat={1}
          clearcoatRoughness={0}
          reflectivity={1}
          envMapIntensity={1}
        />
        
        {/* Selection ring */}
        {isSelected && (
          <mesh scale={[1.5, 1.5, 1.5]}>
            <torusGeometry args={[size * 1.2, 0.2, 16, 100]} />
            <meshBasicMaterial color="#fff" opacity={0.5} transparent />
          </mesh>
        )}
        
        {/* Hover label */}
        {hovered && (
          <Html
            position={[0, size + 2, 0]}
            center
            style={{
              background: 'rgba(0, 0, 0, 0.8)',
              padding: '4px 8px',
              borderRadius: '4px',
              color: 'white',
              fontSize: '12px',
              whiteSpace: 'nowrap',
              pointerEvents: 'none',
            }}
          >
            <div>
              <strong>{device.hostname}</strong>
              <br />
              {device.ipAddress}
              <br />
              {device.deviceType}
            </div>
          </Html>
        )}
      </mesh>
      
      {/* Sparkle effect for online devices */}
      {device.isOnline && (
        <Sparkles
          count={20}
          scale={size * 2}
          size={2}
          speed={0.4}
          color={color}
        />
      )}
    </Float>
  );
};

// Connection Line Component
const ConnectionLine: React.FC<{
  connection: Connection;
  sourcePosition: THREE.Vector3;
  targetPosition: THREE.Vector3;
}> = ({ connection, sourcePosition, targetPosition }) => {
  const lineRef = useRef<THREE.Line>(null);
  
  const curve = useMemo(() => {
    const midPoint = new THREE.Vector3()
      .addVectors(sourcePosition, targetPosition)
      .multiplyScalar(0.5);
    midPoint.y += 10; // Arc height
    
    return new THREE.QuadraticBezierCurve3(
      sourcePosition,
      midPoint,
      targetPosition
    );
  }, [sourcePosition, targetPosition]);

  const points = curve.getPoints(50);
  const geometry = new THREE.BufferGeometry().setFromPoints(points);

  const color = useMemo(() => {
    const colors: { [key: string]: string } = {
      TCP: '#00ACC1',
      UDP: '#4CAF50',
      HTTP: '#FF9800',
      HTTPS: '#9C27B0',
      ICMP: '#F44336',
    };
    return colors[connection.protocol] || '#757575';
  }, [connection.protocol]);

  useFrame((state) => {
    if (lineRef.current && connection.isActive) {
      // Animate line opacity for active connections
      const material = lineRef.current.material as THREE.LineBasicMaterial;
      material.opacity = 0.3 + Math.sin(state.clock.elapsedTime * 2) * 0.2;
    }
  });

  return (
    <>
      <line ref={lineRef} geometry={geometry}>
        <lineBasicMaterial
          color={color}
          opacity={connection.isActive ? 0.5 : 0.2}
          transparent
          linewidth={2}
        />
      </line>
      
      {/* Data flow particles */}
      {connection.isActive && (
        <Trail
          width={2}
          length={10}
          color={color}
          attenuation={(t) => t * t}
        >
          <mesh position={sourcePosition}>
            <sphereGeometry args={[0.3]} />
            <meshBasicMaterial color={color} />
          </mesh>
        </Trail>
      )}
    </>
  );
};

// Background Grid
const NetworkGrid: React.FC = () => {
  return (
    <group>
      <gridHelper
        args={[200, 40]}
        position={[0, -20, 0]}
        material-color="#1a1a2e"
        material-opacity={0.5}
        material-transparent
      />
      <mesh position={[0, -20.1, 0]} rotation={[-Math.PI / 2, 0, 0]}>
        <planeGeometry args={[200, 200]} />
        <meshStandardMaterial
          color="#0a0e27"
          metalness={0.1}
          roughness={0.9}
        />
      </mesh>
    </group>
  );
};

// Main Visualization Component
const NetworkVisualization: React.FC<NetworkVisualizationProps> = ({
  devices,
  connections,
  onDeviceSelect,
  selectedDevice,
}) => {
  const devicePositions = useMemo(() => {
    const positions: { [key: string]: THREE.Vector3 } = {};
    devices.forEach(device => {
      positions[device.id] = new THREE.Vector3(
        device.position.x,
        device.position.y,
        device.position.z
      );
    });
    return positions;
  }, [devices]);

  return (
    <Box sx={{ width: '100%', height: '100%', position: 'relative' }}>
      <Canvas
        camera={{ position: [50, 50, 100], fov: 60 }}
        gl={{ antialias: true, alpha: true }}
        shadows
      >
        {/* Lighting */}
        <ambientLight intensity={0.4} />
        <directionalLight
          position={[50, 50, 50]}
          intensity={1}
          castShadow
          shadow-mapSize={[2048, 2048]}
        />
        <pointLight position={[-50, 50, -50]} intensity={0.5} color="#00ACC1" />
        <pointLight position={[50, 50, -50]} intensity={0.5} color="#FF6F00" />
        
        {/* Environment */}
        <Environment preset="city" />
        <fog attach="fog" args={['#0a0e27', 100, 300]} />
        
        {/* Network Grid */}
        <NetworkGrid />
        
        {/* Connections */}
        {connections.map(connection => {
          const sourcePos = devicePositions[connection.sourceDevice.id];
          const targetPos = devicePositions[connection.targetDevice.id];
          
          if (sourcePos && targetPos) {
            return (
              <ConnectionLine
                key={connection.id}
                connection={connection}
                sourcePosition={sourcePos}
                targetPosition={targetPos}
              />
            );
          }
          return null;
        })}
        
        {/* Devices */}
        {devices.map(device => (
          <DeviceNode
            key={device.id}
            device={device}
            onClick={() => onDeviceSelect(device)}
            isSelected={selectedDevice?.id === device.id}
          />
        ))}
        
        {/* Controls */}
        <OrbitControls
          enablePan={true}
          enableZoom={true}
          enableRotate={true}
          maxDistance={200}
          minDistance={20}
          autoRotate={false}
          autoRotateSpeed={0.5}
        />
        
        {/* Contact Shadows */}
        <ContactShadows
          position={[0, -20, 0]}
          opacity={0.4}
          scale={200}
          blur={2}
        />
      </Canvas>
      
      {/* 3D Controls Help */}
      <Box
        sx={{
          position: 'absolute',
          bottom: 16,
          left: 16,
          backgroundColor: 'rgba(0, 0, 0, 0.6)',
          padding: '8px 12px',
          borderRadius: '8px',
          fontSize: '12px',
          color: 'white',
        }}
      >
        <div>🖱️ Left Click: Select Device</div>
        <div>🖱️ Right Click + Drag: Rotate View</div>
        <div>🖱️ Scroll: Zoom In/Out</div>
      </Box>
    </Box>
  );
};

export default NetworkVisualization; 