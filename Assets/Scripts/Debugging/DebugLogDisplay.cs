using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using MRMotifs.Shared;

namespace MRMotifs.Debugging
{
    /// <summary>
    /// Simple on-screen debug log display that works with the existing DebugLogger system.
    /// Shows categorized debug logs during gameplay - useful for Quest debugging.
    /// </summary>
    public class DebugLogDisplay : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject m_logPanel;
        [SerializeField] private ScrollRect m_scrollRect;
        [SerializeField] private TextMeshProUGUI m_logText;
        [SerializeField] private Button m_toggleButton;
        [SerializeField] private Button m_clearButton;
        [SerializeField] private Button m_exportButton;
        [SerializeField] private Dropdown m_categoryFilter;
        
        [Header("Display Settings")]
        [SerializeField] private bool m_showOnStart = false;
        [SerializeField] private int m_maxDisplayLines = 50;
        [SerializeField] private bool m_autoScrollToBottom = true;
        [SerializeField] private bool m_showTimestamps = true;
        [SerializeField] private string m_filterCategory = ""; // Empty = show all
        
        [Header("Input Settings")]
        [SerializeField] private KeyCode m_toggleKey = KeyCode.F1;
        
        private bool m_isVisible;
        
        private void Start()
        {
            InitializeUI();
            
            // Subscribe to DebugLogger events
            DebugLogger.OnRuntimeLogAdded += OnLogAdded;
            
            // Set initial visibility
            SetLogPanelVisible(m_showOnStart);
            
            // Ensure DebugLogger is initialized
            DebugLogger.InitializeRuntimeLogCollection();
        }
        
        private void OnDestroy()
        {
            DebugLogger.OnRuntimeLogAdded -= OnLogAdded;
        }
        
        private void InitializeUI()
        {
            // Setup buttons
            if (m_toggleButton != null)
                m_toggleButton.onClick.AddListener(ToggleLogPanel);
                
            if (m_clearButton != null)
                m_clearButton.onClick.AddListener(ClearLogs);
                
            if (m_exportButton != null)
                m_exportButton.onClick.AddListener(ExportLogs);
            
            // Setup category filter dropdown
            if (m_categoryFilter != null)
            {
                m_categoryFilter.onValueChanged.AddListener(OnCategoryFilterChanged);
                RefreshCategoryDropdown();
            }
            
            // Initialize log text
            if (m_logText != null)
            {
                m_logText.text = "Debug log display ready...\\n";
            }
        }
        
        private void Update()
        {
            // Handle toggle input
            if (Input.GetKeyDown(m_toggleKey))
            {
                ToggleLogPanel();
            }
            
            // Handle Quest controller input (Y button)
            if (OVRInput.GetDown(OVRInput.Button.Three))
            {
                ToggleLogPanel();
            }
        }
        
        private void OnLogAdded(DebugLogger.RuntimeLogEntry logEntry)
        {
            if (!m_isVisible) return;
            
            // Apply category filter
            if (!string.IsNullOrEmpty(m_filterCategory) && 
                !logEntry.category.Equals(m_filterCategory, System.StringComparison.OrdinalIgnoreCase))
                return;
                
            RefreshDisplay();
        }
        
        private void RefreshDisplay()
        {
            if (m_logText == null) return;
            
            var logs = DebugLogger.RuntimeLogs;
            var filteredLogs = logs.Where(log => 
                string.IsNullOrEmpty(m_filterCategory) || 
                log.category.Equals(m_filterCategory, System.StringComparison.OrdinalIgnoreCase)
            ).TakeLast(m_maxDisplayLines);
            
            var displayText = string.Join("\\n", filteredLogs.Select(FormatLogEntry));
            m_logText.text = displayText;
            
            // Auto-scroll to bottom
            if (m_autoScrollToBottom && m_scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                m_scrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        private string FormatLogEntry(DebugLogger.RuntimeLogEntry logEntry)
        {
            string timestamp = m_showTimestamps ? $"[{logEntry.timestamp:HH:mm:ss}] " : "";
            string categoryColor = GetCategoryColor(logEntry.category);
            return $"{timestamp}<color={categoryColor}>[{logEntry.category}]</color> {logEntry.message}";
        }
        
        private string GetCategoryColor(string category)
        {
            switch (category.ToUpper())
            {
                case "BULLET": return "#FF6B6B";
                case "PLAYER": return "#4ECDC4";
                case "COLOCATION": return "#45B7D1";
                case "NETWORK": return "#96CEB4";
                case "HEALTH": return "#FFEAA7";
                case "GAME": return "#DDA0DD";
                case "SHOOTING": return "#FF69B4";
                case "ERROR": return "#FF4444";
                case "CONFIG": return "#FF4444";
                default: return "#FFFFFF";
            }
        }
        
        public void ToggleLogPanel()
        {
            SetLogPanelVisible(!m_isVisible);
        }
        
        public void SetLogPanelVisible(bool visible)
        {
            m_isVisible = visible;
            
            if (m_logPanel != null)
                m_logPanel.SetActive(visible);
            
            if (visible)
            {
                RefreshDisplay();
                RefreshCategoryDropdown();
            }
        }
        
        private void RefreshCategoryDropdown()
        {
            if (m_categoryFilter == null) return;
            
            var categories = DebugLogger.RuntimeLogs
                .Select(log => log.category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
                
            categories.Insert(0, "ALL");
            
            m_categoryFilter.ClearOptions();
            m_categoryFilter.AddOptions(categories);
        }
        
        private void OnCategoryFilterChanged(int index)
        {
            if (m_categoryFilter == null) return;
            
            var selectedOption = m_categoryFilter.options[index].text;
            m_filterCategory = selectedOption == "ALL" ? "" : selectedOption;
            
            RefreshDisplay();
        }
        
        private void ClearLogs()
        {
            DebugLogger.ClearRuntimeLogs();
            RefreshDisplay();
        }
        
        private void ExportLogs()
        {
            var filePath = DebugLogger.ExportLogsToFile();
            DebugLogger.Game($"Logs exported to: {filePath}");
        }
        
        /// <summary>
        /// Position the log panel for VR viewing.
        /// </summary>
        public void PositionForVR()
        {
            if (m_logPanel != null && Camera.main != null)
            {
                var cameraTransform = Camera.main.transform;
                m_logPanel.transform.position = cameraTransform.position + cameraTransform.forward * 1.5f;
                m_logPanel.transform.rotation = Quaternion.LookRotation(
                    cameraTransform.forward, Vector3.up);
            }
        }
        
        /// <summary>
        /// Show only logs from a specific category.
        /// </summary>
        public void ShowCategory(string category)
        {
            m_filterCategory = category;
            RefreshDisplay();
            
            // Update dropdown if available
            if (m_categoryFilter != null)
            {
                var optionIndex = m_categoryFilter.options.FindIndex(o => o.text == category);
                if (optionIndex >= 0)
                    m_categoryFilter.value = optionIndex;
            }
        }
        
        /// <summary>
        /// Show only error logs.
        /// </summary>
        public void ShowErrorsOnly()
        {
            var errorLogs = DebugLogger.GetRecentErrors(60); // Last hour of errors
            var displayText = string.Join("\\n", errorLogs.Select(FormatLogEntry));
            
            if (m_logText != null)
                m_logText.text = displayText.Length > 0 ? displayText : "No recent errors found.";
        }
    }
}