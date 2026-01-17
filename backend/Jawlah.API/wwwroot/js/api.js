// Jawlah Web Dashboard - API Integration
// This file handles all communication with the backend API

const API_CONFIG = {
    BASE_URL: '/api',  // Relative URL since web is served from same backend
    TOKEN_KEY: 'jawlah_token',
    REFRESH_TOKEN_KEY: 'jawlah_refresh_token',
    USER_KEY: 'jawlah_user'
};

// Security: HTML escape to prevent XSS
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Common logout function
function logout() {
    if (confirm('هل أنت متأكد من تسجيل الخروج؟')) {
        api.clearAuth();
        window.location.href = 'login.html';
    }
}

// Common notification badge loader
async function loadNotificationCount() {
    try {
        const response = await notificationsApi.getUnreadCount();
        const badge = document.getElementById('notifBadge');
        if (badge) {
            badge.style.display = response.data > 0 ? 'block' : 'none';
        }
    } catch (e) {
        // Silently fail - notification badge is not critical
    }
}

// ==================== Core API Functions ====================

const api = {
    // Get stored auth token
    getToken() {
        return localStorage.getItem(API_CONFIG.TOKEN_KEY);
    },

    // Set auth token
    setToken(token) {
        localStorage.setItem(API_CONFIG.TOKEN_KEY, token);
    },

    // Set refresh token
    setRefreshToken(token) {
        localStorage.setItem(API_CONFIG.REFRESH_TOKEN_KEY, token);
    },

    // Get refresh token
    getRefreshToken() {
        return localStorage.getItem(API_CONFIG.REFRESH_TOKEN_KEY);
    },

    // Clear all auth data
    clearAuth() {
        localStorage.removeItem(API_CONFIG.TOKEN_KEY);
        localStorage.removeItem(API_CONFIG.REFRESH_TOKEN_KEY);
        localStorage.removeItem(API_CONFIG.USER_KEY);
    },

    // Check if user is authenticated
    isAuthenticated() {
        return !!this.getToken();
    },

    // Get stored user data
    getUser() {
        const userData = localStorage.getItem(API_CONFIG.USER_KEY);
        return userData ? JSON.parse(userData) : null;
    },

    // Set user data
    setUser(user) {
        localStorage.setItem(API_CONFIG.USER_KEY, JSON.stringify(user));
    },

    // Make API request
    async request(endpoint, options = {}) {
        const url = `${API_CONFIG.BASE_URL}${endpoint}`;

        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };

        // Add auth header if token exists
        const token = this.getToken();
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        try {
            const response = await fetch(url, {
                ...options,
                headers
            });

            // Handle 401 - try to refresh token
            if (response.status === 401) {
                const refreshed = await this.tryRefreshToken();
                if (refreshed) {
                    // Retry the request with new token
                    headers['Authorization'] = `Bearer ${this.getToken()}`;
                    const retryResponse = await fetch(url, { ...options, headers });
                    return await this.handleResponse(retryResponse);
                } else {
                    // Refresh failed - logout
                    this.clearAuth();
                    window.location.href = 'login.html';
                    throw new Error('Session expired');
                }
            }

            return await this.handleResponse(response);
        } catch (error) {
            console.error('API Request Error:', error);
            throw error;
        }
    },

    // Handle API response
    async handleResponse(response) {
        const data = await response.json();

        if (!response.ok) {
            const errorMessage = data.message || data.errors?.join(', ') || 'حدث خطأ في الطلب';
            throw new Error(errorMessage);
        }

        return data;
    },

    // Try to refresh the auth token
    async tryRefreshToken() {
        const refreshToken = this.getRefreshToken();
        if (!refreshToken) return false;

        try {
            const response = await fetch(`${API_CONFIG.BASE_URL}/auth/refresh`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.getToken()}`
                },
                body: JSON.stringify(refreshToken)
            });

            if (response.ok) {
                const data = await response.json();
                if (data.data?.token) {
                    this.setToken(data.data.token);
                    return true;
                }
            }
            return false;
        } catch {
            return false;
        }
    },

    // HTTP Methods
    get(endpoint) {
        return this.request(endpoint, { method: 'GET' });
    },

    post(endpoint, body) {
        return this.request(endpoint, {
            method: 'POST',
            body: JSON.stringify(body)
        });
    },

    put(endpoint, body) {
        return this.request(endpoint, {
            method: 'PUT',
            body: JSON.stringify(body)
        });
    },

    delete(endpoint) {
        return this.request(endpoint, { method: 'DELETE' });
    }
};

// ==================== Auth API ====================

const authApi = {
    // Login with username and password (for supervisors/admins)
    async login(username, password) {
        const response = await api.post('/auth/login', { username, password });

        if (response.data?.token) {
            api.setToken(response.data.token);
            if (response.data.refreshToken) {
                api.setRefreshToken(response.data.refreshToken);
            }
            if (response.data.user) {
                api.setUser(response.data.user);
            }
        }

        return response;
    },

    // Get current user profile
    async getProfile() {
        return await api.get('/auth/profile');
    },

    // Logout
    async logout() {
        try {
            await api.post('/auth/logout');
        } catch (e) {
            console.log('Logout API call failed, clearing local auth');
        }
        api.clearAuth();
        window.location.href = 'login.html';
    },

    // Refresh token
    async refreshToken() {
        const refreshToken = api.getRefreshToken();
        return await api.post('/auth/refresh', refreshToken);
    }
};

// ==================== Dashboard API ====================

const dashboardApi = {
    async getOverview() {
        return await api.get('/dashboard/overview');
    }
};

// ==================== Users API ====================

const usersApi = {
    async getAll(page = 1, pageSize = 50) {
        return await api.get(`/users?page=${page}&pageSize=${pageSize}`);
    },

    async getById(id) {
        return await api.get(`/users/${id}`);
    },

    async getWorkers() {
        return await api.get('/users/by-role/Worker');
    },

    async update(id, data) {
        return await api.put('/users/profile', data);
    },

    async updateStatus(userId, status) {
        return await api.put(`/users/${userId}/status`, { status });
    },

    async assignZones(userId, zoneIds) {
        return await api.post(`/users/${userId}/zones`, { zoneIds });
    },

    async getUserZones(userId) {
        return await api.get(`/users/${userId}/zones`);
    },

    async resetDevice(userId) {
        return await api.post(`/users/${userId}/reset-device`);
    },

    async deleteUser(userId) {
        return await api.delete(`/users/${userId}`);
    }
};

// ==================== Tasks API ====================

const tasksApi = {
    async getAll(params = {}) {
        const query = new URLSearchParams(params).toString();
        return await api.get(`/tasks/all${query ? '?' + query : ''}`);
    },

    async getById(id) {
        return await api.get(`/tasks/${id}`);
    },

    async create(task) {
        return await api.post('/tasks', task);
    },

    async approve(taskId) {
        return await api.put(`/tasks/${taskId}/approve`);
    },

    async reject(taskId, reason) {
        return await api.put(`/tasks/${taskId}/reject`, { reason });
    },

    async deleteTask(taskId) {
        return await api.delete(`/tasks/${taskId}`);
    }
};

// ==================== Zones API ====================

const zonesApi = {
    async getAll() {
        return await api.get('/zones');
    }
};

// ==================== Tracking API ====================

const trackingApi = {
    async getWorkerLocations() {
        return await api.get('/tracking/locations');
    }
};

// ==================== Reports API ====================

const reportsApi = {
    async getTasksReport(startDate, endDate) {
        const params = new URLSearchParams();
        if (startDate) params.append('startDate', startDate);
        if (endDate) params.append('endDate', endDate);
        params.append('format', 'json');
        return await api.get(`/reports/tasks?${params.toString()}`);
    },

    // Download Excel with auth header
    async downloadTasksExcel(startDate, endDate) {
        try {
            const url = `${API_CONFIG.BASE_URL}/reports/tasks?startDate=${startDate}&endDate=${endDate}&format=excel`;
            const response = await fetch(url, {
                headers: { 'Authorization': `Bearer ${api.getToken()}` }
            });
            if (!response.ok) throw new Error('فشل تحميل التقرير');
            const blob = await response.blob();
            const link = document.createElement('a');
            link.href = URL.createObjectURL(blob);
            link.download = `tasks_report_${startDate}.xlsx`;
            link.click();
            URL.revokeObjectURL(link.href);
        } catch (e) {
            alert('فشل تحميل التقرير');
        }
    }
};

// ==================== Notifications API ====================

const notificationsApi = {
    async getAll() {
        return await api.get('/notifications');
    },

    async getUnreadCount() {
        return await api.get('/notifications/unread-count');
    },

    async markAsRead(id) {
        return await api.put(`/notifications/${id}/mark-read`);
    },

    async markAllAsRead() {
        return await api.put('/notifications/mark-all-read');
    }
};

// ==================== Issues API ====================

const issuesApi = {
    async getAll(params = {}) {
        const query = new URLSearchParams(params).toString();
        return await api.get(`/issues${query ? '?' + query : ''}`);
    },

    async getById(id) {
        return await api.get(`/issues/${id}`);
    },

    async updateStatus(issueId, status) {
        return await api.put(`/issues/${issueId}/status`, { status });
    },

    async deleteIssue(issueId) {
        return await api.delete(`/issues/${issueId}`);
    },

    // Update forwarding notes (where the issue was sent)
    async updateForwardingNotes(issueId, notes) {
        return await api.put(`/issues/${issueId}/forwarding-notes`, { notes });
    },

    // Download PDF report for an issue
    async downloadPdf(issueId) {
        try {
            const url = `${API_CONFIG.BASE_URL}/issues/${issueId}/pdf`;
            const response = await fetch(url, {
                headers: { 'Authorization': `Bearer ${api.getToken()}` }
            });
            if (!response.ok) throw new Error('فشل تحميل التقرير');
            const blob = await response.blob();
            const link = document.createElement('a');
            link.href = URL.createObjectURL(blob);
            link.download = `issue_report_${issueId}.pdf`;
            link.click();
            URL.revokeObjectURL(link.href);
            return true;
        } catch (e) {
            console.error('PDF download error:', e);
            throw e;
        }
    }
};

// ==================== Attendance API ====================

const attendanceApi = {
    async getHistory(params = {}) {
        const query = new URLSearchParams(params).toString();
        return await api.get(`/attendance/history${query ? '?' + query : ''}`);
    }
};

// Export to window for global access
window.API_CONFIG = API_CONFIG;
window.api = api;
window.authApi = authApi;
window.dashboardApi = dashboardApi;
window.usersApi = usersApi;
window.tasksApi = tasksApi;
window.zonesApi = zonesApi;
window.trackingApi = trackingApi;
window.reportsApi = reportsApi;
window.notificationsApi = notificationsApi;
window.issuesApi = issuesApi;
window.attendanceApi = attendanceApi;
window.escapeHtml = escapeHtml;
window.logout = logout;
window.loadNotificationCount = loadNotificationCount;
