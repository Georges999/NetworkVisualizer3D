using System;
using System.Collections.Generic;
using System.Net;
using NetworkVisualizer3D.Core.Models;

namespace NetworkVisualizer3D.Core.Utils
{
    /// <summary>
    /// Utility class for detecting device types based on MAC addresses and IP patterns
    /// </summary>
    public class DeviceTypeDetector
    {
        private readonly Dictionary<string, DeviceType> _ouiDatabase;
        private readonly Dictionary<string, DeviceType> _vendorMappings;

        public DeviceTypeDetector()
        {
            _ouiDatabase = InitializeOuiDatabase();
            _vendorMappings = InitializeVendorMappings();
        }

        /// <summary>
        /// Detects device type based on MAC address and IP address
        /// </summary>
        /// <param name="macAddress">MAC address of the device</param>
        /// <param name="ipAddress">IP address of the device</param>
        /// <returns>Detected device type</returns>
        public DeviceType DetectDeviceType(string macAddress, string ipAddress)
        {
            if (string.IsNullOrEmpty(macAddress))
                return DeviceType.Unknown;

            // Extract OUI (first 3 octets) from MAC address
            var oui = ExtractOui(macAddress);
            if (!string.IsNullOrEmpty(oui) && _ouiDatabase.ContainsKey(oui))
            {
                return _ouiDatabase[oui];
            }

            // Check vendor patterns in MAC address
            var vendorType = DetectByVendorPattern(macAddress);
            if (vendorType != DeviceType.Unknown)
            {
                return vendorType;
            }

            // Check IP address patterns
            var ipType = DetectByIpPattern(ipAddress);
            if (ipType != DeviceType.Unknown)
            {
                return ipType;
            }

            return DeviceType.Unknown;
        }

        /// <summary>
        /// Gets the vendor name from MAC address
        /// </summary>
        /// <param name="macAddress">MAC address</param>
        /// <returns>Vendor name or empty string if not found</returns>
        public string GetVendorName(string macAddress)
        {
            var oui = ExtractOui(macAddress);
            return GetVendorByOui(oui);
        }

        private string ExtractOui(string macAddress)
        {
            if (string.IsNullOrEmpty(macAddress) || macAddress.Length < 8)
                return string.Empty;

            // Remove separators and take first 6 characters
            var cleanMac = macAddress.Replace(":", "").Replace("-", "").Replace(".", "").ToUpper();
            return cleanMac.Length >= 6 ? cleanMac.Substring(0, 6) : string.Empty;
        }

        private DeviceType DetectByVendorPattern(string macAddress)
        {
            var oui = ExtractOui(macAddress);
            var vendor = GetVendorByOui(oui).ToLower();

            foreach (var mapping in _vendorMappings)
            {
                if (vendor.Contains(mapping.Key.ToLower()))
                {
                    return mapping.Value;
                }
            }

            return DeviceType.Unknown;
        }

        private DeviceType DetectByIpPattern(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out var ip))
                return DeviceType.Unknown;

            var bytes = ip.GetAddressBytes();
            
            // Common patterns for different device types
            if (bytes.Length == 4) // IPv4
            {
                // Router/Gateway patterns (typically .1, .254)
                if (bytes[3] == 1 || bytes[3] == 254)
                    return DeviceType.Router;

                // Server patterns (often in specific ranges)
                if (IsInServerRange(bytes))
                    return DeviceType.Server;

                // Printer patterns (often in specific ranges)
                if (IsInPrinterRange(bytes))
                    return DeviceType.Printer;
            }

            return DeviceType.Unknown;
        }

        private bool IsInServerRange(byte[] ipBytes)
        {
            // Common server IP ranges
            return (ipBytes[0] == 10 && ipBytes[1] == 0 && ipBytes[2] == 0 && ipBytes[3] < 50) ||
                   (ipBytes[0] == 192 && ipBytes[1] == 168 && ipBytes[2] == 1 && ipBytes[3] < 50);
        }

        private bool IsInPrinterRange(byte[] ipBytes)
        {
            // Common printer IP ranges
            return (ipBytes[0] == 192 && ipBytes[1] == 168 && ipBytes[2] == 1 && ipBytes[3] >= 100 && ipBytes[3] <= 150);
        }

        private string GetVendorByOui(string oui)
        {
            // This is a simplified vendor lookup - in a real implementation,
            // you would use the official IEEE OUI database
            var vendors = new Dictionary<string, string>
            {
                { "000C29", "VMware" },
                { "001C42", "Parallels" },
                { "080027", "VirtualBox" },
                { "00155D", "Microsoft" },
                { "001DD8", "Microsoft" },
                { "00E04C", "Realtek" },
                { "001B63", "Apple" },
                { "3C0754", "Apple" },
                { "F0766F", "Apple" },
                { "B8E856", "Apple" },
                { "00D0C9", "Intel" },
                { "001E67", "Intel" },
                { "7085C2", "Intel" },
                { "00E04B", "3Com" },
                { "0050DA", "3Com" },
                { "001CF0", "Dell" },
                { "B8CA3A", "Dell" },
                { "18A905", "Dell" },
                { "001E4F", "HP" },
                { "009027", "HP" },
                { "70106F", "HP" },
                { "001B78", "Cisco" },
                { "0026CA", "Cisco" },
                { "F866F2", "Cisco" },
                { "00A0C9", "Intel" },
                { "001560", "D-Link" },
                { "0017E2", "D-Link" },
                { "001CF0", "Dell" },
                { "00E081", "Tyan" },
                { "000B6A", "Netgear" },
                { "001E2A", "Netgear" },
                { "A021B7", "Netgear" }
            };

            return vendors.ContainsKey(oui) ? vendors[oui] : "Unknown";
        }

        private Dictionary<string, DeviceType> InitializeOuiDatabase()
        {
            return new Dictionary<string, DeviceType>
            {
                // VMware/Virtual machines
                { "000C29", DeviceType.Computer },
                { "001C42", DeviceType.Computer },
                { "080027", DeviceType.Computer },
                
                // Apple devices
                { "001B63", DeviceType.Computer },
                { "3C0754", DeviceType.MobilePhone },
                { "F0766F", DeviceType.Computer },
                { "B8E856", DeviceType.Computer },
                
                // Network equipment
                { "001B78", DeviceType.Router },
                { "0026CA", DeviceType.Router },
                { "F866F2", DeviceType.Switch },
                
                // Printers
                { "001E4F", DeviceType.Printer },
                { "009027", DeviceType.Printer },
                { "70106F", DeviceType.Printer },
                
                // Servers/Enterprise
                { "001CF0", DeviceType.Server },
                { "B8CA3A", DeviceType.Server },
                { "18A905", DeviceType.Server },
                
                // IoT/Smart devices
                { "000B6A", DeviceType.IoTDevice },
                { "001E2A", DeviceType.IoTDevice },
                { "A021B7", DeviceType.AccessPoint }
            };
        }

        private Dictionary<string, DeviceType> InitializeVendorMappings()
        {
            return new Dictionary<string, DeviceType>
            {
                { "apple", DeviceType.Computer },
                { "cisco", DeviceType.Router },
                { "netgear", DeviceType.Router },
                { "d-link", DeviceType.Router },
                { "linksys", DeviceType.Router },
                { "tp-link", DeviceType.Router },
                { "asus", DeviceType.Router },
                { "hp", DeviceType.Printer },
                { "canon", DeviceType.Printer },
                { "epson", DeviceType.Printer },
                { "brother", DeviceType.Printer },
                { "lexmark", DeviceType.Printer },
                { "dell", DeviceType.Server },
                { "ibm", DeviceType.Server },
                { "lenovo", DeviceType.Computer },
                { "microsoft", DeviceType.Computer },
                { "intel", DeviceType.Computer },
                { "samsung", DeviceType.MobilePhone },
                { "lg", DeviceType.SmartTV },
                { "sony", DeviceType.GameConsole },
                { "nintendo", DeviceType.GameConsole },
                { "xbox", DeviceType.GameConsole },
                { "playstation", DeviceType.GameConsole },
                { "roku", DeviceType.SmartTV },
                { "chromecast", DeviceType.SmartTV },
                { "amazon", DeviceType.IoTDevice },
                { "google", DeviceType.IoTDevice },
                { "nest", DeviceType.IoTDevice },
                { "philips", DeviceType.IoTDevice },
                { "ring", DeviceType.Camera },
                { "arlo", DeviceType.Camera },
                { "hikvision", DeviceType.Camera },
                { "dahua", DeviceType.Camera }
            };
        }
    }
} 