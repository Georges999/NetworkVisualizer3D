using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PacketDotNet;
using NetworkVisualizer3D.Core.Models;

namespace NetworkVisualizer3D.Core.Utils
{
    /// <summary>
    /// Utility class for analyzing HTTP traffic and detecting security threats
    /// </summary>
    public class HttpAnalyzer
    {
        private readonly List<string> _sensitiveDataPatterns;
        private readonly List<string> _sqlInjectionPatterns;
        private readonly List<string> _xssPatterns;
        private readonly List<string> _suspiciousUserAgents;

        public HttpAnalyzer()
        {
            _sensitiveDataPatterns = InitializeSensitiveDataPatterns();
            _sqlInjectionPatterns = InitializeSqlInjectionPatterns();
            _xssPatterns = InitializeXssPatterns();
            _suspiciousUserAgents = InitializeSuspiciousUserAgents();
        }

        /// <summary>
        /// Analyzes a TCP packet for HTTP content and security threats
        /// </summary>
        /// <param name="tcpPacket">TCP packet to analyze</param>
        /// <returns>HTTP analysis result or null if not HTTP traffic</returns>
        public HttpAnalysisResult? AnalyzePacket(TcpPacket tcpPacket)
        {
            if (tcpPacket.PayloadData == null || tcpPacket.PayloadData.Length == 0)
                return null;

            var payload = Encoding.UTF8.GetString(tcpPacket.PayloadData);
            
            // Check if this is HTTP traffic
            if (!IsHttpTraffic(payload, tcpPacket.DestinationPort))
                return null;

            var result = new HttpAnalysisResult();
            
            // Parse HTTP request/response
            ParseHttpContent(payload, result);
            
            // Analyze for security threats
            AnalyzeSecurity(payload, result);
            
            return result;
        }

        /// <summary>
        /// Analyzes HTTP content for sensitive data exposure
        /// </summary>
        /// <param name="content">HTTP content to analyze</param>
        /// <returns>List of security alerts</returns>
        public List<SecurityAlert> AnalyzeSensitiveData(string content)
        {
            var alerts = new List<SecurityAlert>();
            
            foreach (var pattern in _sensitiveDataPatterns)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
                if (matches.Count > 0)
                {
                    alerts.Add(new SecurityAlert
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        Type = AlertType.UnencryptedSensitiveData,
                        Severity = ThreatLevel.High,
                        Title = "Sensitive Data Detected",
                        Description = $"Potential sensitive data found: {GetPatternDescription(pattern)}",
                        Evidence = matches[0].Value.Substring(0, Math.Min(100, matches[0].Value.Length))
                    });
                }
            }
            
            return alerts;
        }

        /// <summary>
        /// Analyzes content for SQL injection attempts
        /// </summary>
        /// <param name="content">Content to analyze</param>
        /// <returns>List of security alerts</returns>
        public List<SecurityAlert> AnalyzeSqlInjection(string content)
        {
            var alerts = new List<SecurityAlert>();
            
            foreach (var pattern in _sqlInjectionPatterns)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
                if (matches.Count > 0)
                {
                    alerts.Add(new SecurityAlert
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        Type = AlertType.SqlInjection,
                        Severity = ThreatLevel.Critical,
                        Title = "SQL Injection Attempt Detected",
                        Description = "Potential SQL injection attack detected in HTTP traffic",
                        Evidence = matches[0].Value.Substring(0, Math.Min(200, matches[0].Value.Length))
                    });
                }
            }
            
            return alerts;
        }

        /// <summary>
        /// Analyzes content for XSS attempts
        /// </summary>
        /// <param name="content">Content to analyze</param>
        /// <returns>List of security alerts</returns>
        public List<SecurityAlert> AnalyzeXss(string content)
        {
            var alerts = new List<SecurityAlert>();
            
            foreach (var pattern in _xssPatterns)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
                if (matches.Count > 0)
                {
                    alerts.Add(new SecurityAlert
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,
                        Type = AlertType.CrossSiteScripting,
                        Severity = ThreatLevel.High,
                        Title = "XSS Attempt Detected",
                        Description = "Potential cross-site scripting attack detected",
                        Evidence = matches[0].Value.Substring(0, Math.Min(200, matches[0].Value.Length))
                    });
                }
            }
            
            return alerts;
        }

        private bool IsHttpTraffic(string payload, int port)
        {
            // Check common HTTP ports
            if (port == 80 || port == 8080 || port == 8000 || port == 3000)
                return true;

            // Check for HTTP headers
            return payload.StartsWith("GET ") || 
                   payload.StartsWith("POST ") || 
                   payload.StartsWith("PUT ") || 
                   payload.StartsWith("DELETE ") || 
                   payload.StartsWith("HEAD ") || 
                   payload.StartsWith("OPTIONS ") ||
                   payload.StartsWith("HTTP/");
        }

        private void ParseHttpContent(string payload, HttpAnalysisResult result)
        {
            var lines = payload.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            if (lines.Length == 0) return;

            var firstLine = lines[0];
            
            // Parse request line
            if (firstLine.StartsWith("GET ") || firstLine.StartsWith("POST ") || 
                firstLine.StartsWith("PUT ") || firstLine.StartsWith("DELETE ") ||
                firstLine.StartsWith("HEAD ") || firstLine.StartsWith("OPTIONS "))
            {
                var parts = firstLine.Split(' ');
                if (parts.Length >= 2)
                {
                    result.Method = parts[0];
                    result.Url = parts[1];
                }
            }

            // Parse headers
            bool inHeaders = true;
            var bodyStart = -1;
            
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                
                if (string.IsNullOrEmpty(line) && inHeaders)
                {
                    inHeaders = false;
                    bodyStart = i + 1;
                    continue;
                }
                
                if (inHeaders && line.Contains(":"))
                {
                    var headerParts = line.Split(new[] { ':' }, 2);
                    if (headerParts.Length == 2)
                    {
                        var headerName = headerParts[0].Trim().ToLower();
                        var headerValue = headerParts[1].Trim();
                        
                        result.Headers[headerName] = headerValue;
                        
                        // Extract specific headers
                        switch (headerName)
                        {
                            case "host":
                                result.Host = headerValue;
                                break;
                            case "user-agent":
                                result.UserAgent = headerValue;
                                break;
                        }
                    }
                }
            }
            
            // Extract body
            if (bodyStart > 0 && bodyStart < lines.Length)
            {
                result.Body = string.Join("\n", lines.Skip(bodyStart));
            }
        }

        private void AnalyzeSecurity(string payload, HttpAnalysisResult result)
        {
            // Check for sensitive data
            result.SecurityAlerts.AddRange(AnalyzeSensitiveData(payload));
            
            // Check for SQL injection
            result.SecurityAlerts.AddRange(AnalyzeSqlInjection(payload));
            
            // Check for XSS
            result.SecurityAlerts.AddRange(AnalyzeXss(payload));
            
            // Check for suspicious user agents
            if (!string.IsNullOrEmpty(result.UserAgent))
            {
                foreach (var suspiciousAgent in _suspiciousUserAgents)
                {
                    if (result.UserAgent.ToLower().Contains(suspiciousAgent.ToLower()))
                    {
                        result.SecurityAlerts.Add(new SecurityAlert
                        {
                            Id = Guid.NewGuid().ToString(),
                            Timestamp = DateTime.UtcNow,
                            Type = AlertType.SuspiciousTraffic,
                            Severity = ThreatLevel.Medium,
                            Title = "Suspicious User Agent",
                            Description = $"Suspicious user agent detected: {suspiciousAgent}",
                            Evidence = result.UserAgent
                        });
                        break;
                    }
                }
            }
            
            // Set flag if sensitive data found
            result.ContainsSensitiveData = result.SecurityAlerts.Any(a => 
                a.Type == AlertType.UnencryptedSensitiveData);
        }

        private List<string> InitializeSensitiveDataPatterns()
        {
            return new List<string>
            {
                // Credit card patterns
                @"\b(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|3[47][0-9]{13}|3[0-9]{13}|6(?:011|5[0-9]{2})[0-9]{12})\b",
                
                // Social Security Number
                @"\b\d{3}-\d{2}-\d{4}\b",
                @"\b\d{9}\b",
                
                // Email addresses
                @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
                
                // Phone numbers
                @"\b\d{3}-\d{3}-\d{4}\b",
                @"\(\d{3}\)\s*\d{3}-\d{4}",
                
                // Passwords in forms
                @"password\s*=\s*['""][^'""]+['""]",
                @"pwd\s*=\s*['""][^'""]+['""]",
                
                // API keys
                @"api[_-]?key\s*[=:]\s*['""][^'""]+['""]",
                @"secret[_-]?key\s*[=:]\s*['""][^'""]+['""]",
                
                // Database connection strings
                @"server\s*=.*password\s*=",
                @"data\s+source\s*=.*password\s*="
            };
        }

        private List<string> InitializeSqlInjectionPatterns()
        {
            return new List<string>
            {
                @"(\%27)|(\')|(\-\-)|(\%23)|(#)",
                @"((\%3D)|(=))[^\n]*((\%27)|(\')|(\-\-)|(\%3B)|(;))",
                @"\w*((\%27)|(\'))((\%6F)|o|(\%4F))((\%72)|r|(\%52))",
                @"((\%27)|(\'))union",
                @"exec(\s|\+)+(s|x)p\w+",
                @"union\s+select",
                @"insert\s+into",
                @"delete\s+from",
                @"drop\s+table",
                @"create\s+table",
                @"alter\s+table",
                @"update\s+\w+\s+set",
                @"select\s+.*\s+from\s+",
                @";\s*(drop|delete|insert|update|create|alter)",
                @"(or|and)\s+1\s*=\s*1",
                @"(or|and)\s+\w+\s*=\s*\w+",
                @"having\s+1\s*=\s*1",
                @"group\s+by\s+\w+\s+having",
                @"order\s+by\s+\d+",
                @"waitfor\s+delay"
            };
        }

        private List<string> InitializeXssPatterns()
        {
            return new List<string>
            {
                @"<script[^>]*>.*?</script>",
                @"javascript:",
                @"vbscript:",
                @"onload\s*=",
                @"onerror\s*=",
                @"onclick\s*=",
                @"onmouseover\s*=",
                @"onfocus\s*=",
                @"onblur\s*=",
                @"onchange\s*=",
                @"onsubmit\s*=",
                @"<iframe[^>]*>",
                @"<object[^>]*>",
                @"<embed[^>]*>",
                @"<link[^>]*>",
                @"<meta[^>]*>",
                @"<img[^>]*onerror",
                @"<svg[^>]*onload",
                @"alert\s*\(",
                @"confirm\s*\(",
                @"prompt\s*\(",
                @"document\.cookie",
                @"document\.write",
                @"eval\s*\(",
                @"setTimeout\s*\(",
                @"setInterval\s*\("
            };
        }

        private List<string> InitializeSuspiciousUserAgents()
        {
            return new List<string>
            {
                "sqlmap",
                "nikto",
                "nmap",
                "masscan",
                "zap",
                "burp",
                "w3af",
                "acunetix",
                "nessus",
                "openvas",
                "metasploit",
                "havij",
                "pangolin",
                "webscarab",
                "paros",
                "httprint",
                "whatweb",
                "dirb",
                "dirbuster",
                "gobuster",
                "wfuzz",
                "ffuf",
                "curl",
                "wget",
                "python-requests",
                "python-urllib",
                "libwww-perl",
                "lwp-trivial",
                "mozilla/4.0 (compatible; msie 6.0; windows nt 5.1)",
                "mozilla/4.0 (compatible; msie 7.0; windows nt 5.1)",
                "bot",
                "crawler",
                "spider",
                "scanner"
            };
        }

        private string GetPatternDescription(string pattern)
        {
            var descriptions = new Dictionary<string, string>
            {
                { @"\b(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|3[47][0-9]{13}|3[0-9]{13}|6(?:011|5[0-9]{2})[0-9]{12})\b", "Credit Card Number" },
                { @"\b\d{3}-\d{2}-\d{4}\b", "Social Security Number" },
                { @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", "Email Address" },
                { @"\b\d{3}-\d{3}-\d{4}\b", "Phone Number" },
                { @"password\s*=\s*['""][^'""]+['""]", "Password Field" },
                { @"api[_-]?key\s*[=:]\s*['""][^'""]+['""]", "API Key" }
            };

            return descriptions.ContainsKey(pattern) ? descriptions[pattern] : "Sensitive Data";
        }
    }
} 