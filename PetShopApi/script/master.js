function ProcesarRespuesta(data) {
    if (data.salidas && Array.isArray(data.salidas)) {
        data.salidas.forEach(item => EnviarMensaje(item.codigo, item.mensaje));
    } else {
        EnviarMensaje(data.codigo, data.mensaje);
    }
}

async function iniciarSesion() {
    const data = await response.json();
    ProcesarRespuesta(data); 
}