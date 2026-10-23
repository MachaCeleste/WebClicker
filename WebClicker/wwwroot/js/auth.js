const AUTH_REGISTER_URL = '/auth/register';
const AUTH_REFRESH_URL = '/auth/refresh';

function isTokenExpired(token) {
    if (!token) return true;
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        const { exp } = JSON.parse(jsonPayload);
        return (Date.now() / 1000) > (exp - 10);
    } catch (e) {
        return true;
    }
}

export async function getValidAccessToken() {
    let accessToken = localStorage.getItem("access_token");
    let refreshToken = localStorage.getItem("refresh_token");

    if (!refreshToken) {
        console.log("No ident found! Registering new user...");
        const res = await fetch(AUTH_REGISTER_URL);
        const data = await res.json();
        saveTokens(data);
        return data.accessToken;
    }

    if (isTokenExpired(accessToken)) {
        console.log("Access token expired! Refreshing token...");
        const res = await fetch(`${AUTH_REFRESH_URL}?refreshToken=${refreshToken}`, { method: 'POST' });
        if (res.ok) {
            const data = await res.json();
            saveTokens(data);
            return data.accessToken;
        } else {
            console.warn("Refresh token invalid! Re-registering...");
            localStorage.clear();
            return getValidAccessToken();
        }
    }

    return accessToken;
}

function saveTokens(data) {
    localStorage.setItem("access_token", data.accessToken);
    localStorage.setItem("refresh_token", data.refreshToken);
}