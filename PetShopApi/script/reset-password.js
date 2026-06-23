document.addEventListener("DOMContentLoaded", async () => {
    const formRestablecer = document.getElementById('formRestablecer');
    const container = document.querySelector('.card');
    const urlParams = new URLSearchParams(window.location.search);
    const tokenActual = urlParams.get('token');
    const tituloReset = document.getElementById('tituloReset');

    const pass1 = document.getElementById('pass1');
    const pass2 = document.getElementById('pass2');
    const btn = document.getElementById('btnGuardar');

    if (!window.location.pathname.includes('reset-password.html')) return;

    const manejarTokenInvalido = (mensaje) => {
        if (tituloReset) tituloReset.innerText = "Enlace no válido";
        
        EnviarMensaje(-1, mensaje);
        
        if (formRestablecer) formRestablecer.style.display = 'none';
    };

    if (!tokenActual) {
        EnviarMensaje(-1, "Data no encontrada. Por favor, solicita uno nuevo.");
        return;
    }

    try {
        const res = await fetch(`${CONFIG.API_BASE_URL}/usuarios/validar-token?token=${encodeURIComponent(tokenActual)}`);
        const response = await res.json();
        const data = response.salida;
        
        if (data.codigo !== 1) {
            manejarTokenInvalido(data.mensaje);
            return;
        }
        
        //EnviarMensaje(1, "Token validado correctamente.");
    } catch (e) {
        manejarTokenInvalido("Error de conexión al validar el token.");
        return;
    }

    pass1.addEventListener('input', validarPassword);
    pass2.addEventListener('input', validarPassword);
    validarPassword();

    formRestablecer.addEventListener('submit', async (e) => {
        e.preventDefault();
        btn.disabled = true;
        btn.innerText = "Guardando...";

        try {
            
            const response = await fetch(`${CONFIG.API_BASE_URL}/usuarios/restablecer-final`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ Token: tokenActual, NuevaPassword: pass1.value })
            });

            const data = await response.json();
            const dataRes = data.salida;

            if (dataRes.codigo === 1) {
                EnviarMensaje(dataRes.codigo, dataRes.mensaje);
                btn.innerText = "Redirigiendo...";
                btn.style.opacity = "0.5";
                console.log("redireccionando")
                setTimeout(() => window.location.href = "index.html", 2000);
            } else {
                EnviarMensaje(dataRes.codigo, dataRes.mensaje);
                btn.disabled = false;
                btn.innerText = "Guardar Cambios";
            }
        } catch (error) {
            EnviarMensaje(-1, "Error al conectar con el servidor.");
            btn.disabled = false;
            btn.innerText = "Guardar Cambios";
        }
    });
});

function validarPassword() {
    const pass1 = document.getElementById('pass1').value;
    const pass2 = document.getElementById('pass2').value;
    const btn = document.getElementById('btnGuardar');
    
    const tieneMayus = /[A-Z]/.test(pass1);
    const tieneMinus = /[a-z]/.test(pass1);
    const tieneNumero = /[0-9]/.test(pass1);
    const tieneLargo = pass1.length >= 8;
    
    actualizarCheck('check-mayus', tieneMayus);
    actualizarCheck('check-minus', tieneMinus);
    actualizarCheck('check-numero', tieneNumero);
    actualizarCheck('check-largo', tieneLargo);
    
    const coinciden = pass1 === pass2 && pass1 !== "";
    const checkCoincide = document.getElementById('check-coincide');
    
    if (pass2 !== "" && !coinciden) {
        checkCoincide.classList.remove('d-none');
    } else {
        checkCoincide.classList.add('d-none');
    }
    
    const esValido = tieneMayus && tieneMinus && tieneNumero && tieneLargo && coinciden;
    btn.disabled = !esValido;
}

function actualizarCheck(id, cumple) {
    const el = document.getElementById(id);
    if (!el) return;
    el.classList.replace(cumple ? 'text-danger' : 'text-success', cumple ? 'text-success' : 'text-danger');
    el.innerHTML = (cumple ? "✓ " : "✕ ") + el.innerText.substring(2);
}