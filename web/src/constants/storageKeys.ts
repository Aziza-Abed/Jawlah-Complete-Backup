// Centralized localStorage key constants
// All localStorage keys used across the app should be defined here

export const STORAGE_KEYS = {
  TOKEN: "followup_token",
  REFRESH_TOKEN: "followup_refresh_token",
  USER: "followup_user",
  DEVICE_ID: "followup_device_id",
  HAS_SEEN_WELCOME: "hasSeenWelcome",
} as const;
