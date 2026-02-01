// FollowUp Web Dashboard - Utility Functions
// Only essential utilities that are actually used

// Format time in Arabic locale
function formatTimeArabic(dateStr) {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleTimeString('ar-SA', { hour: '2-digit', minute: '2-digit' });
}

// Format date in Arabic locale
function formatDateArabic(dateStr) {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('ar-SA', { day: '2-digit', month: 'short' });
}

// Export
window.formatTimeArabic = formatTimeArabic;
window.formatDateArabic = formatDateArabic;
