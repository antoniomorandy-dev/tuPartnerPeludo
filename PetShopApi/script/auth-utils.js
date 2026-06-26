function parseJwt(token) {
    var base64Url = token.split('.')[1];
    var base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(window.atob(base64));
}

function initAuth(onAuthenticatedCallback) {
    const googleToken = localStorage.getItem('google_token');
    const userSession = localStorage.getItem('user_session');

    if (!googleToken && !userSession) {
        window.location.replace("index.html");
        return;
    }

    if (onAuthenticatedCallback) {
        const data = userSession ? JSON.parse(userSession) : parseJwt(googleToken);
        onAuthenticatedCallback(data);
    }
}

function logout() {
    localStorage.removeItem('google_token');
    localStorage.removeItem('user_session');
    localStorage.removeItem('session_token');
    window.location.replace("index.html");
}

// En tu archivo de seguridad (ej. auth-utils.js o al inicio de admin-productos.js)
function protegerRutaAdmin() {
    const sessionData = localStorage.getItem('user_session');
    
    if (!sessionData) {
        window.location.replace("index.html"); // No ha iniciado sesión
        return;
    }

    const session = JSON.parse(sessionData);
    
    // Aquí está el candado:
    if (session.rol === 'cliente') {
        window.location.replace("main.html"); // Obligamos a volver al main
        return;
    }
}

// Llama a esto inmediatamente al cargar admin-productos.html
protegerRutaAdmin();