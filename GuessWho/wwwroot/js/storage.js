/**
 * storage.js — Guess Who? session persistence helpers
 *
 * Stores the current player's session identity (game code, display name, token)
 * in sessionStorage so it can be recovered if the page is reloaded and the URL
 * query parameters are somehow lost (e.g. a user navigates to the bare game URL
 * after a circuit drop).
 *
 * sessionStorage is tab-scoped and cleared when the tab is closed, which is
 * appropriate for an in-progress game session.
 */

const SESSION_KEY = 'guesswho_session';

/**
 * Persists the player's session identity for the given game code.
 * @param {string} code  - 4-character game code
 * @param {string} name  - player display name
 * @param {string} token - player GUID token
 */
export function saveSession(code, name, token) {
    try {
        sessionStorage.setItem(SESSION_KEY, JSON.stringify({ code, name, token }));
    } catch {
        // sessionStorage may be blocked in certain browser configurations — fail silently.
    }
}

/**
 * Loads the stored session identity if it matches the given game code.
 * @param {string} code - the expected game code
 * @returns {{ code: string, name: string, token: string } | null}
 */
export function loadSession(code) {
    try {
        const raw = sessionStorage.getItem(SESSION_KEY);
        if (!raw) return null;
        const data = JSON.parse(raw);
        if (data && data.code === code && data.name && data.token) return data;
        return null;
    } catch {
        return null;
    }
}

/**
 * Removes the stored session identity.
 * Call this when a game ends cleanly so stale data does not interfere with
 * future sessions played in the same browser tab.
 */
export function clearSession() {
    try {
        sessionStorage.removeItem(SESSION_KEY);
    } catch {
        // fail silently
    }
}
