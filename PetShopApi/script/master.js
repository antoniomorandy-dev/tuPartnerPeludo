function ProcesarRespuesta(data) {
    if (data.salidas && Array.isArray(data.salidas) && data.salidas.length > 0) {
        data.salidas.forEach(item => EnviarMensaje(item.codigo, item.mensaje));
    } 
    else if (data.mensaje) {
        EnviarMensaje(data.codigo || 0, data.mensaje);
    }
    else {
        console.warn("La respuesta no contiene mensajes:", data);
    }
}

function EnviarMensaje(codigo, mensaje) {
    toastr.options = { "closeButton": true, "progressBar": true, "positionClass": 'toast-bottom-right' };
    if (codigo <= -1) toastr.error(mensaje);
    else if (codigo === 0) toastr.info(mensaje);
    else toastr.success(mensaje);
}

if (data.codigo === 1) {
    const userSession = {
        nombre: data.user, 
        rol: data.rol
    };
    localStorage.setItem('user_session', JSON.stringify(userSession));
    window.location.href = "main.html";
}

async function realizarLogin(email, password) {
    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/usuarios/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: email, password: password })
        });
        
        const data = await response.json();

        if (data.codigo === 1) {
            const userSession = {
                nombre: data.user,
                rol: data.rol
            };
            localStorage.setItem('user_session', JSON.stringify(userSession));
            window.location.href = "main.html";
        } else {
            ProcesarRespuesta(data);
        }
    } catch (error) {
        EnviarMensaje(-1, "Error de conexión con el servidor");
    }
}